using MetaverseMax.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class BuildingCollection
    {
        public int IP_impact { get; set; }
        public int minIP { get; set; }
        public int maxIP { get; set; }

        public double avgIP { get; set; }

        public int rangeIP { get; set; }

        public string sync_date { get; set; }
        public string building_type { get; set; }
        public int building_lvl { get; set; }
        public int buildingIP_impact { get; set; }

        public int buildings_predict { get; set; }

        public PredictOutcome predict { get; set; }
        public PredictOutcome predict_bonus_bug { get; set; }

        public IEnumerable<BuildingTypeIP> buildings { get; set; }
        public IEnumerable<ResourceTotal> total_produced { get; set; }
        public IEnumerable<ResourceTotal> total_produced_month { get; set; }
        public IEnumerable<ResourceTotal> total_produced_excess { get; set; }
        public IEnumerable<ResourceTotal> total_produced_month_excess { get; set; }
    }


    public class PredictOutcome{
        public int correct { get; set; }
        public decimal correct_percent { get; set; }
        public int miss { get; set; }
        public decimal miss_percent { get; set; }
        public int miss_above { get; set; }
        public decimal miss_above_percent { get; set; }
        public int miss_below { get; set; }
        public decimal miss_below_percent { get; set; }
    }
}
