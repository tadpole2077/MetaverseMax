using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.Database
{
    public class BlockchainTransaction
    {
        [Key]
        [Column("transaction")]
        public int transaction { get; set; }

        [Column("hash")]
        public string hash { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("action")]
        public char action { get; set; }

        [Column("event_recorded_utc")]
        public DateTime event_recorded_utc { get; set; }

        [Column("amount", TypeName = "decimal(16, 8)")]
        public decimal amount { get; set; }

        [Column("approval_recorded_utc")]
        public DateTime? approval_recorded_utc { get; set; }

        [Column("approval_amount", TypeName = "decimal(16, 8)")]
        public decimal? approval_amount { get; set; }

        [Column("note")]
        public string note { get; set; }
    }
}
