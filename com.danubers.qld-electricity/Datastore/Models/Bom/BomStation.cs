using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Danubers.QldElectricity.Datastore.Models.Bom
{
    public class BomStation
    {
        public long Id { get; set; }
        public string Wmo { get; set; }
        public string HistoryProduct { get; set; }
        public string Name { get; set; }
    }
}
