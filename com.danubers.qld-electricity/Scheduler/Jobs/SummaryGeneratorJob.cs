using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Danubers.QldElectricity.Datastore.Models.Bom;
using Danubers.QldElectricity.Injection;
using Danubers.QldElectricity.Scheduler;
using Dapper;
using FluentScheduler;

namespace Danubers.QldElectricity.Jobs
{
    public class SummaryGeneratorJob : AsyncJobShim
    {
        private readonly IDataProvider _dataProvider;

        public SummaryGeneratorJob(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        private async Task GeneratePowerSummary()
        {
            //Dirty atm, just loading in all entries and doing processing from there. Need some queries to better manage this from a DB level
            List<EnergexPayload> rawReadings;
            using (var conn = _dataProvider.GetConnection())
            {
                rawReadings = (await conn.QueryAsync<EnergexPayload>("SELECT * FROM Energex")).ToList();
            }

            var averageReadings = new Dictionary<Tuple<int, int>, PowerStat>();
            foreach (var hour in Enumerable.Range(0,23))
            {
                foreach (var minute in Enumerable.Range(0, 5).Select(r => r * 10))
                {
                    var time = new Tuple<int, int>(hour, minute);
                    var timeReading =
                        rawReadings.Where(
                            r =>
                                r.Timestamp.Hour == hour && r.Timestamp.Minute >= minute &&
                                r.Timestamp.Minute < minute + 10).ToList();

                    if (!timeReading.Any())
                        continue;
                    var stat = new PowerStat(){Mean = timeReading.Average(r => r.Value)};
                    averageReadings.Add(time, stat);
                }
            }

            using (var conn = _dataProvider.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    var query =
                        "REPLACE INTO PowerSummary (Hour, Minute, Day, Value) VALUES (@day, @minute, @hour, @value)";
                    foreach (var reading in averageReadings)
                    {
                        await conn.ExecuteAsync(query,
                            new {day = (string)null, hour = reading.Key.Item1, minute = reading.Key.Item2, value = reading.Value.Mean});
                    }
                    transaction.Commit();
                }
            }

        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await GeneratePowerSummary();
        }
    }

    internal class PowerStat
    {
        public float? Mean { get; set; }
    }
}