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
        Task<IOrderedEnumerable<WeatherDataPoint>> GetWeatherData();
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

        public Task<IOrderedEnumerable<WeatherDataPoint>> GetWeatherData()
        {
            throw new NotImplementedException();
        }
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
