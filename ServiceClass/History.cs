using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class HistoryProduction
    {
        public int seq { get; set; }
        public string run_datetime { get; set; }
        public long run_datetimeDT { get; set; }
        public int building_lvl { get; set; }
        public int building_type { get; set; }
        public int amount_produced { get; set; }
        public string building_product { get; set; }
        public int building_product_id { get; set; }
        public int efficiency_p { get; set; }
        public int efficiency_m { get; set; }
        public decimal efficiency_c { get; set; }
        public decimal efficiency_c_60 { get; set; }
        public int building_ip { get; set; }
        public decimal poi_bonus { get; set; }
        public PetUsage pet_usage { get; set; }
        public bool is_perk_activated { get; set; }
        public int influence_bonus { get; set; }
    }

    public class PetUsage
    {
        public int strength { get; set; }
        public int endurance { get; set; }
        public int intelligence { get; set; }
        public int charisma { get; set; }
        public int luck { get; set; }
        public int agility { get; set; }

    }

    public class BuildingHistory
    {   
        public int slowdown { get; set; }
        public string owner_matic { get; set; }
        public string start_production { get; set; }
        public int run_count { get; set; }
        public int current_building_lvl { get; set; }
        public int current_building_product_id { get; set; }
        public int current_building_id { get; set; }
        public int current_building_type { get; set; }
        public IEnumerable<ResourceTotal> totalProduced { get; set; }
        public IEnumerable<HistoryProduction> detail { get; set; }        

        public string prediction_product { get; set; }
        public int prediction_base_min { get; set; }
        public int prediction_max { get; set; }
        public int prediction_range { get; set; }
        public IEnumerable<string> changes_last_run { get; set; }

        public Prediction prediction { get; set; }
        //public Prediction prediction_bonus_bug { get; set; }
        public bool use_prediction { get; set; }
        public bool check_citizens { get; set; }
        public int damage { get; set; }
        public decimal damage_eff { get; set; }
        public decimal damage_eff_rounded { get; set; }
    }

    public class Prediction
    {
        public int product_id { get; set; }
        public int ip { get; set; }
        public int influance { get; set; }
        public int influance_bonus { get; set; }

        public int efficiency_p { get; set; }
        public int efficiency_m { get; set; }

        public int cit_range_percent { get; set; }
        public decimal cit_efficiency { get; set; }
        public decimal cit_efficiency_partial { get; set; }
        public decimal cit_efficiency_rounded { get; set; }
        public decimal cit_produce { get; set; }
        public decimal cit_produce_rounded { get; set; }
        public decimal cit_produce_max { get; set; }

        public bool is_perk_activated { get; set; }
        public decimal ip_efficiency { get; set; }        
        public decimal ip_efficiency_partial { get; set; }
        public int ip_range_percent { get; set; }        
        public int ip_efficiency_rounded { get; set; }
        public decimal ip_produce { get; set; }
        public decimal ip_produce_rounded { get; set; }
        public decimal ip_produce_max { get; set; }

        public decimal ip_and_cit_percent { get; set; }
        public decimal ip_and_cit_percent_100 { get; set; }
        public decimal ip_and_cit_percent_rounded { get; set; }
        public decimal ip_and_cit_produce { get; set; }
        public decimal ip_and_cit_produce_rounded { get; set; }

        public decimal ip_and_cit_percent_dmg { get; set; }
        public int ip_and_cit_percent_rounded_dmg { get; set; }
        public decimal ip_and_cit_produce_dmg { get; set; }
        public decimal ip_and_cit_produce_dmg_rounded { get; set; }

        public int resource_lvl { get; set; }
        public int resource_range_percent { get; set; }
        public int resource_lvl_percent { get; set; }
        public decimal resource_partial { get; set; }
        public decimal resource_lvl_range { get; set; }
        public decimal resource_lvl_produce { get; set; }
        public int resource_lvl_produce_rounded { get; set; }

        public int subtotal_rounded { get; set; }
        public decimal subtotal { get; set; }

        public decimal poi_bonus { get; set; }
        public decimal poi_bonus_produce { get; set; }
        public int poi_bonus_produce_rounded { get; set; }

        public int total { get; set; }
        public string total_note { get; set; }

        public decimal total_decimal { get; set; }

        public int total_100 { get; set; }
        public decimal total_decimal_100 { get; set; }
    }

    public class ResourceTotal
    {
        public int resourceId { get; set; }
        public long total { get; set; }
        public string totalFormat { get; set; }
        public string name { get; set; }
        public int buildingCount { get; set; }
        public int buildingActive { get; set; }
        public string buildingImg { get; set; }
        public int buildingId { get; set; }
        public string buildingName { get; set; }
        public int buildingSeq { get; set; }

    }
}
