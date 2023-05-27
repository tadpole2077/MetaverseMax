using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("AlertTrigger")]
    public class AlertTrigger
    {
        [Key]
        [Column("alert_key")]
        public int alert_key { get; set; }

        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("key_type")]
        public int key_type { get; set; }

        [Column("id")]
        public int id { get; set; }

        [Column("last_updated")]
        public DateTime last_updated { get; set; }

    }
}
