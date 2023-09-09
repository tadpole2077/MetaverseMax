using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("District")]
    public class District
    {
        [Key]
        [Column("district_key")]
        public int district_key { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("last_update")]
        public DateTime last_update { get; set; }

        [Column("update_instance")]
        public int update_instance { get; set; }

        [Column("owner_name")]
        public string owner_name { get; set; }

        [Column("owner_avatar_id")]
        public int? owner_avatar_id { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("active_from")]
        public DateTime? active_from { get; set; }



        [Column("land_count")]
        public int land_count { get; set; }

        [Column("plots_claimed")]
        public int plots_claimed { get; set; }

        [Column("building_count")]
        public int building_count { get; set; }

        [Column("energy_plot_count")]
        public int? energy_plot_count { get; set; }

        [Column("industry_plot_count")]
        public int? industry_plot_count { get; set; }

        [Column("production_plot_count")]
        public int? production_plot_count { get; set; }

        [Column("office_plot_count")]
        public int? office_plot_count { get; set; }

        [Column("residential_plot_count")]
        public int? residential_plot_count { get; set; }

        [Column("commercial_plot_count")]
        public int? commercial_plot_count { get; set; }

        [Column("municipal_plot_count")]
        public int? municipal_plot_count { get; set; }

        [Column("poi_plot_count")]
        public int? poi_plot_count { get; set; }


        [Column("energy_tax")]
        public int? energy_tax { get; set; }

        [Column("production_tax")]
        public int? production_tax { get; set; }

        [Column("commercial_tax")]
        public int? commercial_tax { get; set; }

        [Column("citizen_tax")]
        public int? citizen_tax { get; set; }


        [Column("construction_energy_tax")]
        public int? construction_energy_tax { get; set; }

        [Column("construction_industry_production_tax")]
        public int? construction_industry_production_tax { get; set; }

        [Column("construction_commercial_tax")]
        public int? construction_commercial_tax { get; set; }

        [Column("construction_municipal_tax")]
        public int? construction_municipal_tax { get; set; }

        [Column("construction_residential_tax")]
        public int? construction_residential_tax { get; set; }

        [Column("resource_zone")]
        public int resource_zone { get; set; }

        [Column("district_matic_key")]
        public string district_matic_key { get; set; }

        [Column("distribution_period")]
        public int? distribution_period { get; set; }

        [Column("insurance_commission")]
        public int insurance_commission { get; set; }

        [Column("land_plot_price")]
        public int? land_plot_price{ get; set; }

    }
}
