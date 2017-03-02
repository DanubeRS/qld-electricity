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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        private readonly IHostingEnvironment _env;
        private readonly SqliteConfiguration _config;

        public SQLiteDataProvider(ILoggerFactory loggerFactory, IOptions<DatastoreConfig> config, IHostingEnvironment env)
        {
            _loggerFactory = loggerFactory;
            _env = env;
            if (config.Value.Type != "sqlite")
                throw new Exception("Bad config");  //TODO

            _config = config.Value.Configuration.Get<SqliteConfiguration>();
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

                var filePath = _config.File;

                logger.LogTrace($"Setting DB path to \"{filePath}\"");

                var sb = new SqliteConnectionStringBuilder
                {
                    DataSource = filePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                };
                _connectionString = sb.ConnectionString;
                logger.LogTrace($"Setting DB connection string to \"{_connectionString}\"");

                logger.LogTrace("Checking if DB exists");
                if (!File.Exists(filePath))
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
                                        "CREATE TABLE Energex (Timestamp INTEGER NOT NULL, Type TEXT NOT NULL, Value REAL NOT NULL)");
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE BomSites" +
                                        "(Wmo STRING NOT NULL," +
                                        "HistoryProduct TEXT NOT NULL," +
                                        "Name TEXT NOT NULL)");
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE BomReadings (" +
                                        "Timestamp INTEGER NOT NULL," +
                                        "SiteId STRING NOT NULL," +
                                        "AirTemp REAL," +
                                        "Dewpoint REAL," +
                                        "CloudOktas INT," +
                                        "WindSpeed INT," +
                                        "WindDir TEXT," +
                                        "FOREIGN KEY(SiteId) REFERENCES BomSites(Id))"
                                    );
                                    await connection.ExecuteAsync(
                                        "CREATE TABLE PowerSummary (" +
                                        "Hour INT NOT NULL," +
                                        "Minute INT NOT NULL," +
                                        "Day INT NOT NULL," +
                                        "Value REAL," +
                                        "CONSTRAINT pk_Key PRIMARY KEY (Hour, Minute, Day)" +
                                        ")"
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

    internal class SqliteConfiguration
    {
        public string File { get; set; }
    }
}
