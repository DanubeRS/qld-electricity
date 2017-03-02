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

            var averageReadings = new Dictionary<Timeslot, PowerStat>();

            foreach (var day in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>())
            {
                //Calculate averages for minutes
                foreach (var hour in Enumerable.Range(0, 24))
                {
                    {
                        //Overall hourly averages
                        var time = new Timeslot() {Hour = hour};
                            var timeReading =
                                rawReadings.Where(
                                    r =>
                                        r.Timestamp.Hour == hour).ToList();

                        if (!timeReading.Any())
                            continue;
                        var stat = new PowerStat() { Mean = timeReading.Average(r => r.Value) };
                        averageReadings.Add(time, stat);
                    }
                    {
                        //Daily hourly averages
                        var time = new Timeslot() { Hour = hour, Day = day};
                        var timeReading =
                            rawReadings.Where(
                                r =>
                                    r.Timestamp.Hour == hour && r.Timestamp.DayOfWeek == day).ToList();

                        if (!timeReading.Any())
                            continue;
                        var stat = new PowerStat() { Mean = timeReading.Average(r => r.Value) };
                        averageReadings.Add(time, stat);
                    }
                    foreach (var minute in Enumerable.Range(0, 6).Select(r => r * 10))
                    {
                        {
                            //Overall minute averages
                            var time = new Timeslot() {Hour = hour, Minute = minute};
                            var timeReading =
                                rawReadings.Where(
                                    r =>
                                        r.Timestamp.Hour == hour && r.Timestamp.Minute >= minute &&
                                        r.Timestamp.Minute < minute + 10).ToList();

                            if (!timeReading.Any())
                                continue;
                            var stat = new PowerStat() {Mean = timeReading.Average(r => r.Value)};
                            averageReadings.Add(time, stat);
                        }

                        //Daily minute averages
                        {
                            var time = new Timeslot() {Day = day, Hour = hour, Minute = minute};
                            var timeReading =
                                rawReadings.Where(
                                    r =>
                                        r.Timestamp.Hour == hour && r.Timestamp.Minute >= minute &&
                                        r.Timestamp.Minute < minute + 10 && 
                                        r.Timestamp.DayOfWeek == day).ToList();

                            if (!timeReading.Any())
                                continue;
                            var stat = new PowerStat() {Mean = timeReading.Average(r => r.Value)};
                            averageReadings.Add(time, stat);
                        }
                    }
                }
            }

            using (var conn = _dataProvider.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    //Check if value exists
                    var query =
                        "REPLACE INTO PowerSummary (Day, Minute, Hour, Value) VALUES (@day, @minute, @hour, @value)";
                    foreach (var reading in averageReadings)
                    {
                        await conn.ExecuteAsync(query,
                            new {day = (reading.Key.Day.HasValue ? (int?)reading.Key.Day.Value : null) ?? -1, hour = reading.Key.Hour, minute = reading.Key.Minute, value = reading.Value.Mean});
                    }
                    transaction.Commit();
                }
            }
            averageReadings = null;
            rawReadings = null;
            GC.Collect();
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await GeneratePowerSummary();
        }
    }

    internal class Timeslot : IEquatable<Timeslot>
    {
        public DayOfWeek? Day { get; set; }
        public int Hour { get; set; } = -1;
        public int Minute { get; set; } = -1;
        public bool Equals(Timeslot other)
        {
            return Day == other.Day && Hour == other.Hour && Minute == other.Minute;
        }
    }

    internal class PowerStat
    {
        public float? Mean { get; set; }
    }
}