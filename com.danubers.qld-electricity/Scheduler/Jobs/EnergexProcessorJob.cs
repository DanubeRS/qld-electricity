using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Danubers.QldElectricity.Injection;
using Danubers.QldElectricity.Scheduler;
using Dapper;
using FluentScheduler;
using Microsoft.Extensions.Logging;

namespace Danubers.QldElectricity.Jobs
{
    public class EnergexProcessorJob : AsyncJobShim
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataProvider _dataProvider;

        public EnergexProcessorJob(ILoggerFactory loggerFactory, IDataProvider dataProvider)
        {
            _loggerFactory = loggerFactory;
            _dataProvider = dataProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var logger = _loggerFactory.CreateLogger("ServiceRunner");
            var httpClient = new HttpClient();
            var lastResponseTime = DateTime.UtcNow;
            var lastResponse = int.MinValue;
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
        }
    }
}