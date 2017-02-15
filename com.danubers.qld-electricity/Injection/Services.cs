using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Dapper;
using Microsoft.Extensions.Logging;

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

            builder.RegisterType<EnergexProcessor>().As<IBackgroundProcessor>();
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
                                            "INSERT INTO Data (Timestamp, Type, Value) VALUES (@Timestamp, @Type, @Value)",
                                            new DataRowPayload("Energex", responseInt));
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
                    await Task.Delay(5000, ct);
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

    internal class DataRowPayload
    {
        public DataRowPayload(string energex, int responseInt)
        {
            Type = energex;
            Timestamp = DateTime.UtcNow;
            Value = responseInt;
        }

        public string Type { get; }
        public DateTime Timestamp { get; }
        public float Value { get; }
    }
}
