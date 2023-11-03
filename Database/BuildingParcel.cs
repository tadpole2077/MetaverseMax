using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    public class BuildingParcel
    {        
        [Key]
        [Column("parcel_id")]
        public int parcel_id { get; set; }

        [Column("plot_count")]
        public int plot_count { get; set; }

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

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("current_price", TypeName = "decimal(16, 4)")]
        public decimal current_price { get; set; }

        [Column("parcel_info_id")]
        public int? parcel_info_id { get; set; }

        [Column("parcel_unit_count")]
        public int? parcel_unit_count { get; set; }

        [Column("building_name")]
        public string building_name { get; set; }

        [Column("building_category_id")]
        public int? building_category_id { get; set; }

        [Column("unit_forsale_count")]
        public int? unit_forsale_count { get; set; }

        [Column("unit_price_low_mega", TypeName = "decimal(16, 2)")]
        public decimal? unit_price_low_mega { get; set; }

        [Column("unit_price_high_mega", TypeName = "decimal(16, 2)")]
        public decimal? unit_price_high_mega { get; set; }

        [Column("unit_price_low_coin", TypeName = "decimal(16, 6)")]
        public decimal? unit_price_low_coin { get; set; }

        [Column("unit_price_high_coin", TypeName = "decimal(16, 6)")]
        public decimal? unit_price_high_coin { get; set; }

        [Column("unit_sale_smallest_size")]
        public int? unit_sale_smallest_size { get; set; }

        [Column("unit_sale_largest_size")]
        public int? unit_sale_largest_size { get; set; }

        [Column("floor_count")]
        public int? floor_count { get; set; }

    }
}
