
namespace MetaverseMax.ServiceClass
{
    public class CitizenAction
    {
        public DateTime? action_datetime { get; set; }
        public int action_type { get; set; }
        public int pet_token_id { get; set; }
        public int land_token_id { get; set; }
        public int citizen_token_id { get; set; }
        public string owner_matic_key { get; set; }
        public string new_owner_key { get; set; }
    }

    public class CitizenWebCollection
    {
        public string last_updated { get; set; }
        public IEnumerable<CitizenWeb> citizen { get; set; }

        public int slowdown { get; set; }
    }

    public class CitizenWeb
    {
        public int token_id { get; set; }
        public string name { get; set; }
        public string generation { get; set; }
        public int breeding { get; set; }
        public string sex { get; set; }
        public int trait_agility { get; set; }
        public int trait_agility_pet { get; set; }
        public int trait_intelligence { get; set; }
        public int trait_intelligence_pet { get; set; }
        public int trait_charisma { get; set; }
        public int trait_charisma_pet { get; set; }
        public int trait_endurance { get; set; }
        public int trait_endurance_pet { get; set; }
        public int trait_luck { get; set; }
        public int trait_luck_pet { get; set; }
        public int trait_strength { get; set; }
        public int trait_strength_pet { get; set; }
        public double trait_avg { get; set; }
        public int max_stamina { get; set; }
        public bool on_sale { get; set; }
        public decimal current_price { get; set; }
        public double efficiency_industry { get; set; }
        public double efficiency_production { get; set; }
        public double efficiency_energy_water { get; set; }
        public double efficiency_energy_electric { get; set; }
        public double efficiency_office { get; set; }
        public double efficiency_commercial { get; set; }
        public double efficiency_municipal { get; set; }

        public int district_id { get; set; }
        public int pos_x { get; set; }
        public int pos_y { get; set; }
        public string building_img { get; set; }
        public string building_desc { get; set; }
        public int building_level { get; set; }
        public string building { get; set; }
        public int pet_token_id { get; set; }

    }
}
