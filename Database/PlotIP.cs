using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("PlotIP")]
    public class PlotIP
    {
        [Column("influence")]
        public int? influence { get; set; }

        [Column("influence_info")]
        public int? influence_info { get; set; }

        [Column("influence_bonus")]
        public int? influence_bonus { get; set; }

        [Column("app_123_bonus")]
        public int? app_123_bonus { get; set; }

        [Column("app_4_bonus")]
        public int? app_4_bonus { get; set; }

        [Column("app_5_bonus")]
        public int? app_5_bonus { get; set; }

        [NotMapped]
        [Column("total_ip")]
        public int total_ip { get; set; }

        [Column("production_poi_bonus", TypeName = "decimal(6, 2)")]
        public decimal? production_poi_bonus { get; set; }

        [Column("last_updated")]
        public DateTime last_updated { get; set; }

        [Column("is_perk_activated")]
        public bool? is_perk_activated { get; set; }

        [Column("building_type_id")]
        public int building_type_id { get; set; }

        [Column("building_id")]
        public int building_id { get; set; }

        [Column("building_level")]
        public int building_level { get; set; }


    }
}
