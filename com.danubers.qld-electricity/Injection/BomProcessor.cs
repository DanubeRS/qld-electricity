using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Danubers.QldElectricity.Datastore.Models.Bom;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Danubers.QldElectricity.Injection
{

    public class BomProcessor : IBackgroundProcessor
    {
        private readonly ILoggerFactory _logFactory;
        private readonly IDataProvider _dataProvider;
        private CancellationTokenSource _cts;
        private Task _task;

        public BomProcessor(ILoggerFactory logFactory, IDataProvider dataProvider)
        {
            _logFactory = logFactory;
            _dataProvider = dataProvider;
            _cts = new CancellationTokenSource();
        }

        public bool Running { get; }

        public Task Start(CancellationToken ct)
        {
            _task = Task.Run(async () =>
            {
                var logger = _logFactory.CreateLogger<BomProcessor>();
                while (!_cts.IsCancellationRequested)
                {
                    ImmutableArray<BomStation> stations;
                    using (logger.BeginScope("PollForStations"))
                    {
                        logger.LogTrace("Opening connection to DB");
                        using (var conn = _dataProvider.GetConnection())
                        {
                            logger.LogDebug("Requesting stations");
                            try
                            {
                                stations = (await conn.QueryAsync<BomStation>("SELECT * FROM BomSites")).ToImmutableArray();
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
                                            var responseModel = JsonConvert.DeserializeObject<BomStationResponseModel>(responseString);
                                            logger.LogInformation($"Found {responseModel.Observations.Header.First().ID}. Latest Temp {responseModel.Observations.Readings.OrderBy(r => r.Timestamp).First().AirTemp} at {responseModel.Observations.Readings.OrderBy(r => r.Timestamp).First().Timestamp}");
                                        }
                                        else
                                        {
                                            logger.LogWarning("Recieved a non-200 code from service. {0} - {1}", response.StatusCode, response.ReasonPhrase);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        logger.LogError("Failed to get station data. {0}", e);
                                        continue;
                                    }

                                }
                            }
                        }
                    }
                    await Task.Delay(60000, _cts.Token);
                }
                logger.LogDebug("BOM polling successfully cancelled.");
                return;
            }, _cts.Token);
            return Task.FromResult(true);
        }

        public async Task Stop(CancellationToken ct)
        {
            _cts.Cancel();
            await _task;
        }
    }
}
