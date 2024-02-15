using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    //[Keyless]       // Available from EF Core 5 
    [Table("OwnerUni")]
    public class OwnerUni
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("owner_uni_id")]
        public int owner_uni_id { get; set; }

        [Column("balance", TypeName = "decimal(16, 8)")]
        public decimal? balance { get; set; }

        [Column("balance_visible")]
        public bool? balance_visible { get; set; }

        [Column("created_date")]
        public DateTime? created_date { get; set; }

        [Column("last_update")]
        public DateTime? last_updated { get; set; }

        [Column("linked_wallet_count")]
        public int linked_wallet_count { get; set; }

        [Column("allow_link")]
        public bool allow_link { get; set; }

    }
}
