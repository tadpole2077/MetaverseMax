using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Transaction")]
    public class Transaction
    {
        [Key]
        [Column("transaction_key")]
        public int transaction_key { get; set; }

        [Column("from_wallet")]
        public string from_wallet { get; set; }

        [Column("to_wallet")]
        public string to_wallet { get; set; }

        [Column("unit_type")]
        public int unit_type { get; set; }

        [Column("unit_amount")]
        public int unit_amount { get; set; }

        [Column("value")]
        public decimal value { get; set; }

        [Column("hash")]
        public string hash { get; set; }

        [Column("sent_time")]
        public DateTime sent_time { get; set; }

        [Column("status")]
        public int status { get; set; }

        [Column("blockchain")]
        public int blockchain { get; set; }

    }
}
