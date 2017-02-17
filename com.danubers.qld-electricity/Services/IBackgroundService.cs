using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
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
        private readonly IEnumerable<IBackgroundProcessor> _processors;

        public DefaultBackgroundService(ILoggerFactory loggerFactory, IDataProvider datastore, IEnumerable<IBackgroundProcessor> processors)
        {
            _loggerFactory = loggerFactory;
            _datastore = datastore;
            _processors = processors;
        }

        public async Task Initiate()
        {
            var logger = _loggerFactory.CreateLogger<IBackgroundService>();
            using (logger.BeginScope("Initiation"))
            {
                logger.LogDebug("Initiating background service");
                logger.LogTrace("Checking if datastore is ready");
                if (!_datastore.IsReady())
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
            //TODO
            //Tidy up services!
            foreach (var process in _processors)
            {
                await process.Start(ct);
            }

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
            }

            foreach (var process in _processors)
            {
                await process.Stop(CancellationToken.None);
            }

        }
    }

    internal interface IBackgroundProcessor
    {
        bool Running { get; }
        Task Start(CancellationToken ct);
        Task Stop(CancellationToken ct);
    }

    public interface IDataProvider
    {
        bool IsReady();
        Task Initialise();
        IDbConnection GetConnection();
    }

    class SQLiteDataProvider : IDataProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public SQLiteDataProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _initialised = false;
        }

        private bool _initialised;
        private string _connectionString;

        public bool IsReady()
        {
            return _initialised;
        }

        public async Task Initialise()
        {
            var logger = _loggerFactory.CreateLogger<SQLiteDataProvider>();
            using (logger.BeginScope("Initialisation"))
            {
                logger.LogDebug("Begin initialisation.");
                var path =
                    Path.Combine(
                        Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application
                            .ApplicationBasePath, "data.db");
                logger.LogTrace($"Setting DB path to \"{path}\"");

                var sb = new SqliteConnectionStringBuilder
                {
                    DataSource = path,
                    Mode = SqliteOpenMode.ReadWriteCreate
                };
                _connectionString = sb.ConnectionString;
                logger.LogTrace($"Setting DB connection string to \"{_connectionString}\"");

                logger.LogTrace("Checking if DB exists");
                if (!File.Exists(path))
                {
                    logger.LogDebug("DB does not exist. Creating.");
                    using (logger.BeginScope("Create Tables"))
                    {
                        logger.LogDebug("Initialising tables.");
                        logger.LogTrace("Opening connection.");
                        using (var connection = GetConnection())
                        {
                            logger.LogTrace("Creating data table.");
                            try
                            {
                                await connection.ExecuteAsync(
                                    "CREATE TABLE Energex (Id INTEGER PRIMARY KEY, Timestamp INTEGER NOT NULL, Type STRING NOT NULL, Value REAL NOT NULL)");
                                await connection.ExecuteAsync(
                                    "CREATE TABLE BomSites" +
                                    "(Id STRING PRIMARY KEY," +
                                    "Wmo STRING NOT NULL," +
                                    "HistoryProduct STRING NOT NULL,"+
                                    "Name STRING NOT NULL)");
                                await connection.ExecuteAsync(
                                    "CREATE TABLE BomReadings (" +
                                    "Id INTEGER PRIMARY KEY," +
                                    "Timestamp INTEGER NOT NULL," +
                                    "SiteId STRING NOT NULL," +
                                    "AirTemp REAL," +
                                    "Dewpoint REAL," +
                                    "CloudOktas INT," +
                                    "WindSpeed INT," +
                                    "WindDir STRING)"
                                );
                            }
                            catch (Exception e)
                            {
                                logger.LogCritical($"Failed to intialise database. {e.Message}");
                                throw;
                            }
                        }
                        _initialised = true;
                    }
                }
                else
                {
                    logger.LogDebug("DB already exists. Continuing.");
                }
            }
        }

        public IDbConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}
