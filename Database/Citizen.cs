using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Citizen")]
    public class Citizen : CitizenTrait
    {
        [Key]
        [Column("token_id")]
        public int token_id { get; set; }

        [Column("name")]
        public string name { get; set; }

        [Column("generation")]
        public int generation { get; set; }

        [Column("breeding")]
        public int breeding { get; set; }

        [Column("sex")]
        public short? sex { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("on_sale_key")]
        public int on_sale_key { get; set; }

        [Column("current_price", TypeName = "decimal(18, 2)")]
        public decimal? current_price { get; set; }

        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("max_stamina")]
        public int max_stamina { get; set; }

        [Column("create_date")]
        public DateTime? create_date { get; set; }

        [Column("last_update")]
        public DateTime? last_update { get; set; }

        [Column("refresh_history")]
        public bool refresh_history { get; set; }

    }

    public class CitizenChange
    {
        public bool historyRefresh { get; set; }

        public bool updateFound { get; set; }

    }
}
