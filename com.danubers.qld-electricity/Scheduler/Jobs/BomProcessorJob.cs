using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Danubers.QldElectricity.Datastore.Models.Bom;
using Danubers.QldElectricity.Injection;
using Danubers.QldElectricity.Scheduler;
using Dapper;
using FluentScheduler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Danubers.QldElectricity.Jobs
{
    public class BomProcessorJob : AsyncJobShim
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataProvider _dataProvider;

        public BomProcessorJob(ILoggerFactory loggerFactory, IDataProvider dataProvider)
        {
            _loggerFactory = loggerFactory;
            _dataProvider = dataProvider;
        }

        private DateTime TimestampConverter(string inputString)
        {
            int year;
            int month;
            int day;
            int hour;
            int minute;

            var @throw = new Action(() => { throw new JsonReaderException("Incorrect string format"); });
            //Parse values
            if (!int.TryParse(inputString.Substring(0, 4), out year))
                @throw();
            if (!int.TryParse(inputString.Substring(4, 2), out month))
                @throw();
            if (!int.TryParse(inputString.Substring(6, 2), out day))
                @throw();
            if (!int.TryParse(inputString.Substring(8, 2), out hour))
                @throw();
            if (!int.TryParse(inputString.Substring(10, 2), out minute))
                @throw();
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        private async Task<BomReading> GetLatestReading(long siteId)
        {
            using (var conn = _dataProvider.GetConnection())
            {
                var readings =
                    (await conn.QueryAsync<BomReading>(
                        "SELECT * FROM BomReadings WHERE SiteId == @Id ORDER BY Timestamp DESC LIMIT 1", new { Id = siteId }))
                    .ToArray();
                return !readings.Any() ? null : readings.First();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {

            var logger = _loggerFactory.CreateLogger<BomProcessor>();
            ImmutableArray<BomStation> stations;
            using (logger.BeginScope("PollForStations"))
            {
                logger.LogTrace("Opening connection to DB");
                using (var conn = _dataProvider.GetConnection())
                {
                    logger.LogDebug("Requesting stations");
                    try
                    {
                        stations =
                            (await conn.QueryAsync<BomStation>("SELECT *, ROWID as Id FROM BomSites")).ToImmutableArray();
                    }
                    catch (Exception e)
                    {
                        logger.LogCritical("Failed to get station data. {0}", e.Message);
                        throw;
                    }
                    logger.LogDebug($"Got {stations.Count()} stations");
                }
            }

            using (logger.BeginScope("PollStations"))
            {
                var sw = new Stopwatch();
                using (var httpClient = new HttpClient())
                {
                    foreach (var station in stations)
                    {
                        using (logger.BeginScope("PollStation"))
                        {
                            sw.Start();
                            try
                            {
                                var response = await httpClient.GetAsync(
                                    $"http://www.bom.gov.au/fwo/{station.HistoryProduct}/{station.HistoryProduct}.{station.Wmo}.json",
                                    ct);
                                sw.Stop();
                                if (response.IsSuccessStatusCode)
                                {
                                    var responseString = await response.Content.ReadAsStringAsync();
                                    var responseModel =
                                        JsonConvert.DeserializeObject<BomStationResponseModel>(responseString);
                                    logger.LogInformation(
                                        $"Found {responseModel.Observations.Header.First().StationName}. Latest Temp {responseModel.Observations.Readings.OrderBy(r => r.Timestamp).First().AirTemp} at {TimestampConverter(responseModel.Observations.Readings.OrderBy(r => r.Timestamp).First().Timestamp).ToLocalTime()}");
                                    IEnumerable<BomStationReadingModel> readings =
                                        responseModel.Observations.Readings.OrderBy(o => o.SortOrder).ToArray();
                                    var currentReading = await GetLatestReading(station.Id);
                                    if (currentReading != null && TimestampConverter(readings.First().Timestamp) ==
                                        currentReading.Timestamp)
                                        continue;
                                    if (currentReading != null)
                                    {
                                        readings =
                                            readings.Where(
                                                r =>
                                                {
                                                    try
                                                    {
                                                        var convertedTimestamp = TimestampConverter(r.Timestamp);
                                                        return convertedTimestamp > currentReading.Timestamp;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        logger.LogWarning(
                                                            $"Failed to convert timestamp {r.Timestamp} to DateTime. {e.Message}");
                                                        throw;
                                                    }
                                                }).ToArray();
                                    }
                                    var convertedReadingsToAdd = readings.Select(reading => new BomReading()
                                    {
                                        Timestamp = TimestampConverter(reading.Timestamp),
                                        AirTemp = reading.AirTemp,
                                        CloudOktas = reading.CloudOktas,
                                        DewPoint = reading.Dewpoint,
                                        SiteId = station.Id,
                                        WindDir = reading.WindDir,
                                        WindSpeed = reading.WindSpeed
                                    });

                                    using (var conn = _dataProvider.GetConnection())
                                    {
                                        foreach (var reading in convertedReadingsToAdd)
                                        {
                                            await conn.ExecuteAsync(
                                                "INSERT INTO BomReadings (Timestamp, SiteId, AirTemp, DewPoint, CloudOktas, WindSpeed, WindDir) VALUES (@Timestamp, @SiteId, @AirTemp, @DewPoint, @CloudOktas, @WindSpeed, @WindDir)",
                                                reading);
                                        }
                                    }
                                }
                                else
                                {
                                    logger.LogWarning("Recieved a non-200 code from service. {0} - {1}",
                                        response.StatusCode, response.ReasonPhrase);
                                }
                            }
                            catch (Exception e)
                            {
                                logger.LogError("Failed to get station data. {0}", e);
                            }

                        }
                    }
                }
            }
            logger.LogInformation("Bom station data retrieval successfully completed.");
        }
    }
}