using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Danubers.QldElectricity.Services;
using Dapper;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Danubers.QldElectricity.Injection
{
    public class Services : Module
    {
        public Services()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultBackgroundService>().As<IBackgroundService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<DataService>().As<IDataService>().SingleInstance();

            builder.RegisterType<EnergexProcessor>().As<IBackgroundProcessor>();
            builder.RegisterType<BomProcessor>().As<IBackgroundProcessor>();
        }
    }

    internal class EnergexProcessor : IBackgroundProcessor
    {
        private readonly ActionBlock<CancellationToken> _getterAction;
        private readonly IDataProvider _dataProvider;
        private readonly ILoggerFactory _loggerFactory;

        public EnergexProcessor(IDataProvider dataProvider, ILoggerFactory loggerFactory)
        {
            Running = false;
            _dataProvider = dataProvider;
            _loggerFactory = loggerFactory;

            //Action
            _getterAction = new ActionBlock<CancellationToken>(async ct =>
            {
                var logger = _loggerFactory.CreateLogger("ServiceRunner");
                var httpClient = new HttpClient();
                var lastResponseTime = DateTime.UtcNow;
                var lastResponse = int.MinValue;
                while (!ct.IsCancellationRequested)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var result = await httpClient.GetAsync(
                        "https://www.energex.com.au/static/Energex/Network%20Demand/networkdemand.txt", ct);
                    sw.Stop();
                    if (result.IsSuccessStatusCode)
                    {
                        var responseString = await result.Content.ReadAsStringAsync();
                        int responseInt;
                        if (int.TryParse(responseString, out responseInt))
                        {
                            logger.LogTrace($"({sw.ElapsedMilliseconds}ms) {responseString}MWh");
                            if (lastResponse != responseInt)
                            {
                                var currentTime = DateTime.UtcNow;
                                var delta = lastResponse > 0 ? $"(Changed {responseInt - lastResponse}) " : string.Empty;
                                var logString =
                                    $"({sw.ElapsedMilliseconds}ms) {responseString}MWh {delta}- Changed {(currentTime - lastResponseTime)} ago";
                                logger.LogInformation(logString);
                                lastResponseTime = currentTime;
                                lastResponse = responseInt;

                                using (var connection = _dataProvider.GetConnection())
                                {
                                    try
                                    {
                                        logger.LogDebug("Logging to Datastore");
                                        await connection.ExecuteAsync(
                                            "INSERT INTO Energex (Timestamp, Type, Value) VALUES (@Timestamp, @Type, @Value)",
                                            new EnergexPayload("Energex", responseInt));
                                        logger.LogDebug("Successfully logged");
                                    }
                                    catch (Exception e)
                                    {
                                        logger.LogCritical($"Failed to log to db. {e.Message}");
                                    }
                                }
                            }
                        }
                    }
                    await Task.Delay((int)TimeSpan.FromMinutes(5).TotalMilliseconds, ct);
                }
            });
        }

        public bool Running { get; private set; }

        public async Task Start(CancellationToken ct)
        {
            Running = true;
            _getterAction.Post(CancellationToken.None);
        }

        public async Task Stop(CancellationToken ct)
        {
            _getterAction.Complete();
        }
    }

    internal class BomStationResponseModel
    {
        [JsonProperty("observations")]
        public BomStationsObservationsModel Observations { get; set; }
    }

    internal class BomStationsObservationsModel
    {
        [JsonProperty(PropertyName = "header")]
        public IEnumerable<BomStationResponseModelHeaderModel> Header { get; set; }
        [JsonProperty(PropertyName = "data")]
        public IEnumerable<BomStationReadingModel> Readings { get; set; }
    }

    internal class BomStationReadingModel
    {

        [JsonProperty("aifstime_utc")]
//        [JsonConverter(typeof(AIFSTImeConverter))]
        public string Timestamp { get; set; }
        [JsonProperty("sort_order")]
        public int SortOrder { get; set; }
        [JsonProperty("air_temp")]
        public float? AirTemp { get; set; }
        [JsonProperty("dewpt")]
        public float? Dewpoint { get; set; }
        [JsonProperty("cloud_oktas")]
        public int? CloudOktas { get; set; }
        [JsonProperty("wind_spd_kmh")]
        public int? WindSpeed { get; set; }
        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }
    }

    internal class BomStationResponseModelHeaderModel
    {
        [JsonProperty(PropertyName = "ID")]
        public string ID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string StationName { get; set; }
        [JsonProperty(PropertyName = "time_zone")]
        public string TimeZone { get; set; }

    }

    internal class EnergexPayload
    {
        public EnergexPayload() { }
        public EnergexPayload(string energex, int responseInt)
        {
            this.Type = energex;
            Timestamp = DateTime.UtcNow;
            Value = responseInt;
        }

        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
    }
}
