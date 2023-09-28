using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("CustomBuilding")]
    public class CustomBuilding
    {        
        [Key]
        [Column("parcel_info_id")]
        public int parcel_info_id { get; set; }

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