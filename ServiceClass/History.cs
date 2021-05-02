using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class HistoryProduction
    {
        public string run_datetime { get; set; }
        public int amount_produced { get; set; }
        public string buildingProduct { get; set; }
        public int efficiency_p { get; set; }
        public int efficiency_m { get; set; }
    }

    public class BuildingHistory
    {
        public string startProduction { get; set; }
        public int runCount { get; set; }
        public IEnumerable<string> totalProduced { get; set; }
        public IEnumerable<HistoryProduction> detail { get; set; }
    }

    public class ResourceTotal
    {
        public int resourceId { get; set; }
        public long resourceTotal { get; set; }
        public string resouceName { get; set; }
    }
}
