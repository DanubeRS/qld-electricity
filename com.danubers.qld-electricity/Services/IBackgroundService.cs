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
using Danubers.QldElectricity.Datastore.Models.Bom;
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

        public DefaultBackgroundService(ILoggerFactory loggerFactory, IDataProvider datastore)
        {
            _loggerFactory = loggerFactory;
            _datastore = datastore;
        }

        public async Task Initiate()
        {
        }

        public void Dispose()
        {
        }

        public async Task RunServices(CancellationToken ct)
        {
            //TODO
        }
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
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                logger.LogTrace("Creating data table.");
                                try
                                {
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE Energex (Timestamp INTEGER NOT NULL, Type STRING NOT NULL, Value REAL NOT NULL)");
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE BomSites" +
                                        "(Wmo STRING NOT NULL," +
                                        "HistoryProduct STRING NOT NULL," +
                                        "Name STRING NOT NULL)");
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE BomReadings (" +
                                        "Timestamp INTEGER NOT NULL," +
                                        "SiteId STRING NOT NULL," +
                                        "AirTemp REAL," +
                                        "Dewpoint REAL," +
                                        "CloudOktas INT," +
                                        "WindSpeed INT," +
                                        "WindDir STRING," +
                                        "FOREIGN KEY(SiteId) REFERENCES BomSites(Id))"
                                    );
                                    var bomStations =
                                        new[]
                                        {
                                            new BomStation()
                                            {
                                                Id = 1,
                                                Name = "Brisbane AP",
                                                HistoryProduct = "IDQ60801",
                                                Wmo = "94578"
                                            }
                                        };

                                    foreach (var station in bomStations)
                                    {
                                        await connection.ExecuteAsync(
                                            "INSERT INTO BomSites (rowid, Wmo, HistoryProduct, Name) VALUES (@Id, @Wmo, @HistoryProduct, @Name)",
                                            station);
                                    }
                                    transaction.Commit();
                                }
                                catch
                                (Exception e)
                                {
                                    logger.LogCritical($"Failed to intialise database. {e.Message}");
                                    transaction.Rollback();
                                    throw;
                                }
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
