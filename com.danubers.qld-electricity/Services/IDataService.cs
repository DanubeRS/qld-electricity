using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Danubers.QldElectricity.Injection;
using Dapper;

namespace Danubers.QldElectricity.Services
{
    public interface IDataService
    {
        Task<IOrderedEnumerable<EnergyDataPoint>> GetEnergyData(DateTime? startTime = null, DateTime? endTime = null);
        Task<IOrderedEnumerable<WeatherDataPoint>> GetWeatherData(string stationId, DateTime? startTime = null, DateTime? endTime = null);
    }

    public class DataService : IDataService
    {
        private readonly IDataProvider _dataProvider;

        public DataService(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public async Task<IOrderedEnumerable<EnergyDataPoint>> GetEnergyData(DateTime? startTime = null, DateTime? endTime = null)
        {
            IOrderedEnumerable<EnergexPayload> values;
            var query = "SELECT * FROM Energex WHERE 1 == 1";

            //Optional limiters
            if (startTime.HasValue)
                query += " AND Timestamp >= @startTime";
            if (endTime.HasValue)
                query += " AND Timestamp <= @endTime";

            var @params = new
            {
                startTime,
                endTime
            };

            using (var conn = _dataProvider.GetConnection())
            {
                values = (await conn.QueryAsync<EnergexPayload>(query, @params)).OrderBy(e => e.Timestamp);
            }
            return values.Select(v => new EnergyDataPoint
            {
                Timestamp = v.Timestamp,
                Value = new EnergyDataValue { Consumption = (int)v.Value }
            }).OrderBy(v => v.Timestamp);
        }

        public async Task<IOrderedEnumerable<WeatherDataPoint>> GetWeatherData(string siteId, DateTime? startTime = null, DateTime? endTime = null)
        {
            IOrderedEnumerable<WeatherDataPayload> values;
            var query = "SELECT * FROM BomReadings WHERE 1 == 1";

            //Optional limiters
            if (!string.IsNullOrEmpty(siteId))
                query += " AND SiteId == @siteId";
            if (startTime.HasValue)
                query += " AND Timestamp >= @startTime";
            if (endTime.HasValue)
                query += " AND Timestamp <= @endTime";

            var @params = new
            {
                startTime,
                endTime,
                siteId
            };

            using (var conn = _dataProvider.GetConnection())
            {
                values = (await conn.QueryAsync<WeatherDataPayload>(query, @params)).OrderBy(e => e.Timestamp);
            }
            return values.Select(v => new WeatherDataPoint()
            {
                Timestamp = v.Timestamp,
                Value = new WeatherDataValue() {Temperature = v.AirTemp}
            }).OrderBy(v => v.Timestamp);
        }
    }

    public class WeatherDataPayload
    {
        public DateTime Timestamp { get; set; }
        public float AirTemp { get; set; } 
    }

    public class WeatherDataPoint : IDataPoint<WeatherDataValue>
    {
        public DateTime Timestamp { get; internal set; }
        public WeatherDataValue Value { get; internal set; }
    }

    public class WeatherDataValue : IDataValue
    {
        public float Temperature { get; internal set; }
    }

    public class EnergyDataPoint : IDataPoint<EnergyDataValue>
    {
        public DateTime Timestamp { get; internal set; }
        public EnergyDataValue Value { get; internal set; }
    }

    public class EnergyDataValue : IDataValue
    {
        public int Consumption { get; internal set; }
    }

    internal interface IDataPoint<T> where T : IDataValue
    {
        DateTime Timestamp { get; }
        T Value { get; }
    }

    internal interface IDataValue
    {
    }
}
