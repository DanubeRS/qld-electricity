using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Danubers.QldElectricity
{
    interface IBackgroundService : IDisposable
    {
        Task Initiate();
        Task RunServices(CancellationToken ctsToken);
    }

    class DefaultBackgroundService : IBackgroundService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataProvider _datastore;

        public DefaultBackgroundService(ILoggerFactory loggerFactory, IDataProvider datastore)
        {
            _loggerFactory = loggerFactory;
            _datastore = datastore;
        }

        public async Task Initiate()
        {
            var logger = _loggerFactory.CreateLogger<IBackgroundService>();
            using (logger.BeginScope("Initiation"))
            {
                logger.LogDebug("Initiating background service");
                logger.LogTrace("Checking if datastore is ready");
                if (_datastore.IsReady())
                {
                    logger.LogTrace("Datastore not ready. Initialising.");
                    logger.LogDebug("Initiating datastore");
                    await _datastore.Initialise();
                }
                else
                {
                    logger.LogTrace("Datastore is already initialised");
                }
                logger.LogInformation("Background service initiated");
            }
        }

        public void Dispose()
        {
        }

        public async Task RunServices(CancellationToken ct)
        {
            var logger = _loggerFactory.CreateLogger("ServiceRunner");
            var httpClient = new HttpClient();
            var lastResponseTime = DateTime.UtcNow;
            var lastResponse = string.Empty;
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
                    logger.LogTrace($"({sw.ElapsedMilliseconds}ms) {responseString}MWh");
                    if (responseString != lastResponse)
                    {
                        var currentTime = DateTime.UtcNow;
                        logger.LogWarning($"({sw.ElapsedMilliseconds}ms) {responseString}MWh - Changed {(currentTime - lastResponseTime)} ago");
                        lastResponseTime = currentTime;
                        lastResponse = responseString;
                    }
                }
                await Task.Delay(5000, ct);
            }
        }
    }

    internal interface IDataProvider
    {
        bool IsReady();
        Task Initialise();
        IDbConnection GetConnection();
    }

    class SQLiteDataProvider : IDataProvider
    {
        private bool _initialised;

        public bool IsReady()
        {
            return _initialised;
        }

        public async Task Initialise()
        {
            await Task.Delay(1000);
            _initialised = true;
        }

        public IDbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
}
