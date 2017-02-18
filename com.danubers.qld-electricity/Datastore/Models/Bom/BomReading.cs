using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Danubers.QldElectricity.Datastore.Models.Bom
{
    public class BomReading
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public long SiteId { get; set; }
        public float? AirTemp { get; set; }
        public float? DewPoint { get; set; }
        public int? CloudOktas { get; set; }
        public int? WindSpeed { get; set; }
        public string WindDir { get; set; }
    }
}
