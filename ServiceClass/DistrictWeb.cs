using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class DistrictWeb
    {
        public int update_instance { get; set; }
        public string last_updateFormated { get; set; }
        public DateTime last_update { get; set; }
        public int district_id { get; set; }
        public string district_name { get; set; }
        public string owner_name { get; set; }
        public int owner_avatar_id { get; set; }
        public string owner_url { get; set; }
        public string owner_matic { get; set; }
        public string active_from { get; set; }
        public int land_count { get; set; }
        public int plots_claimed { get; set; }
        public int building_count { get; set; }
        public int energy_count { get; set; }
        public int industry_count { get; set; }
        public int production_count { get; set; }
        public int office_count { get; set; }
        public int residential_count { get; set; }
        public int commercial_count { get; set; }
        public int municipal_count { get; set; }
        public int poi_count { get; set; }

        public int energy_tax { get; set; }
        public int production_tax { get; set; }
        public int commercial_tax { get; set; }
        public int citizen_tax { get; set; }

        public int construction_energy_tax { get; set; }
        public int construction_industry_production_tax { get; set; }
        public int construction_commercial_tax { get; set; }
        public int construction_municipal_tax { get; set; }
        public int construction_residential_tax { get; set; }

        public int resource_zone { get; set; }
        public int district_matic_key { get; set; }
        public int distribution_period { get; set; }
        public int insurance_commission { get; set; }
        public string promotion { get; set; }
        public string promotion_start { get; set; }
        public string promotion_end { get; set; }

        public NgxChart produceTax { get; set; }
        public NgxChart constructTax { get; set; }
        public NgxChart fundHistory { get; set; }
        public NgxChart distributeHistory { get; set; }

        public IEnumerable<Perk> perkSchema { get; set; }
        public IEnumerable<DistrictPerk> districtPerk { get; set; }

    }
}
