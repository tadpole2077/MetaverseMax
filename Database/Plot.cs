using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Plot")]
    public class Plot
    {
        [Key]
        [Column("plot_id")]
        public int plot_id { get; set; }

        [Column("update_type")]
        public int? update_type { get; set; }

        [Column("cell_id")]
        public int cell_id { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("pos_x")]
        public int pos_x { get; set; }

        [Column("pos_y")]
        public int pos_y { get; set; }

        [Column("last_action")]
        public DateTime? last_action { get; set; }

        [Column("last_action_type")]
        public int last_action_type { get; set; }

        [Column("last_updated")]
        public DateTime? last_updated { get; set; }

        [Column("notes")]
        public string notes { get; set; }

        [Column("unclaimed_plot")]
        public bool unclaimed_plot { get; set; }

        [Column("owner_nickname")]
        public string owner_nickname { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("owner_avatar_id")]
        public int owner_avatar_id { get; set; }

        [Column("land_type")]
        public int land_type { get; set; }

        [Column("resources")]
        public int resources { get; set; }

        [Column("building_id")]
        public int building_id { get; set; }

        [Column("building_level")]
        public int building_level { get; set; }

        [Column("building_type_id")]
        public int building_type_id { get; set; }

        [Column("token_id")]
        public int token_id { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("for_rent", TypeName = "decimal(16, 4)")]
        public decimal for_rent { get; set; }

        [Column("rented")]
        public bool rented { get; set; }

        [Column("current_price", TypeName = "decimal(16, 4)")]
        public decimal current_price { get; set; }

        [Column("abundance")]
        public int? abundance { get; set; }

        [Column("building_abundance")]
        public int? building_abundance { get; set; }

        [Column("condition")]
        public int? condition { get; set; }

        [Column("influence_info")]
        public int? influence_info { get; set; }


        [Column("current_influence_rank", TypeName = "decimal(6, 2)")]
        public decimal? current_influence_rank { get; set; }


        [Column("last_run_produce_date")]
        public DateTime? last_run_produce_date { get; set; }

        [Column("last_run_produce")]
        public int? last_run_produce { get; set; }

        [Column("last_run_produce_id")]
        public int last_run_produce_id { get; set; }

        [Column("last_run_produce_predict")]
        public bool last_run_produce_predict { get; set; }

        [Column("predict_produce")]
        public int? predict_produce { get; set; }

        [Column("influence")]
        public int? influence { get; set; }

        [Column("influence_bonus")]
        public int? influence_bonus { get; set; }

        [Column("influence_poi_bonus")]
        public Boolean? influence_poi_bonus { get; set; }



        [Column("app_4_bonus")]
        public int? app_4_bonus { get; set; }

        [Column("app_5_bonus")]
        public int? app_5_bonus { get; set; }

        [Column("app_123_bonus")]
        public int? app_123_bonus { get; set; }

        [Column("production_poi_bonus", TypeName = "decimal(6, 2)")]
        public decimal production_poi_bonus { get; set; }

        [Column("is_perk_activated")]
        public Boolean? is_perk_activated { get; set; }

        [Column("low_stamina_alert")]
        public Boolean? low_stamina_alert { get; set; }

        // Current production type within this building - Mapping to BUILDING_PRODUCT Enum.
        // Only populated for Industry,  for Production, Energy the type of product produced is extracted from building type, meaning only one product possible.
        [Column("action_id")]
        public int action_id { get; set; }

        [Column("poi_active_until")]
        public DateTime? poi_active_until { get; set; }

        [Column("citizen_count")]
        public int? citizen_count { get; set; }

        [NotMapped]
        public List<int> citizen { get; set; }

        // flag indicate plot was upgraded since last sync - potential a upgrade to huge / Mega - resulting in new building encompasing multiple prior plots under one token.
        [NotMapped]
        public bool upgradedSinceLastSync { get; set; }

        [Column("parcel_id")]
        public int parcel_id { get; set; }

        [Column("parcel_info_id")]
        public int parcel_info_id { get; set; }

    }

    public class PlotCord
    {
        public int plotId { get; set; }
        public int posX { get; set; }
        public int posY { get; set; }
    }
}
