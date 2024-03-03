using System.Collections.Generic;

namespace MetaverseMax.ServiceClass
{
    public class BuildingCollection
    {
        public bool show_prediction { get; set; }
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

        public OfficeSummary office_summary { get; set; }

        public int active_count { get; set; }

        public string img_url { get; set; }
        public IEnumerable<ResourceActiveWeb> active_buildings { get; set; }
        public IEnumerable<BuildingIPWeb> buildings { get; set; }
        public IEnumerable<ResourceTotal> total_produced { get; set; }
        public IEnumerable<ResourceTotal> total_produced_month { get; set; }
    }

    public class ResourceActiveWeb
    {
        public int resource_id { get; set; }
        public string name { get; set; }
        public int total { get; set; }
        public int active { get; set; }
        public long active_total_ip { get; set; }
        public int shutdown { get; set; }
        public string building_img { get; set; }
        public int building_id { get; set; }
        public string building_name { get; set; }
    }

    public class PredictOutcome
    {
        public int correct { get; set; }
        public decimal correct_percent { get; set; }
        public int miss { get; set; }
        public decimal miss_percent { get; set; }
        public int miss_above { get; set; }
        public decimal miss_above_percent { get; set; }
        public int miss_below { get; set; }
        public decimal miss_below_percent { get; set; }
    }

    public class OfficeSummary
    {
        public long active_total_ip { get; set; }
        public decimal active_max_daily_distribution_per_ip { get; set; }
    }
    public class BuildingIPWeb
    {
        public int id { get; set; }

        public int pos { get; set; }

        public int pos_x { get; set; }

        public int pos_y { get; set; }

        public decimal rank { get; set; }

        public int ip_t { get; set; }

        public int ip_b { get; set; }

        public int ip_i { get; set; }

        public int bon { get; set; }

        public string name { get; set; }

        public string name_m { get; set; }

        public int nid { get; set; }

        public int con { get; set; }

        // Active Produce - flag used to control building Running icon, Also used in Predict Eval column - controls if tick is shown when column view is enabled.
        // Active Office or Commercial, contains min citizens with min stamina.
        public int act { get; set; }

        // active stamina. Has min Citizens assigned, with min stamina.
        public int acts { get; set; }
        // out of stamina flag.
        public int oos { get; set; }

        public int pre { get; set; }

        public int dis { get; set; }

        public string warn { get; set; }

        public string img { get; set; }

        public decimal price { get; set; }

        public int ren { get; set; }

        public decimal r_p { get; set; }

        public decimal poi { get; set; }

        public int tax { get; set; }

        public int r { get; set; }

        // Building_id - used for filtering by building subtype.
        public int bid { get; set; }

        // Building Ranking Alert
        public int al { get; set; }
    }

    public class BuildingToken
    {
        public int token_id { get; set; }
        public int building_level { get; set; }
        public int building_type_id { get; set; }
    }
}
