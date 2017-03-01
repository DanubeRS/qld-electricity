using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Danubers.QldElectricity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ConsoleApplication
{
#pragma warning disable CS1591
    public class Program
    {
        private static ILogger<Program> _logger;

        public static void Main(string[] args)
        {
            //Initiate configuration

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls("http://*:80", "http://localhost:5000")
                .Build();

            _logger = host.Services.GetService<ILoggerFactory>().CreateLogger<Program>();
            var cts = new CancellationTokenSource();
            Task.Run(async () =>

            {
                _logger.LogDebug("Starting QldElectricity Application");
                using (var backgroundService = host.Services.GetService<IBackgroundService>())
                {
                    _logger.LogInformation("Calling background service initiation");
                    await backgroundService.Initiate();
                    _logger.LogInformation("Running host");
                    var background = backgroundService.RunServices(cts.Token);
                    host.Run(cts.Token);
                    cts.Cancel();

                    await background;
                    _logger.LogInformation("Host requested close");
                }

            }, cts.Token).GetAwaiter().GetResult();
        }
    }
}