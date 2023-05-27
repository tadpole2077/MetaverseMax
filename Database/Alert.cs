using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Alert")]
    public class Alert
    {
        [Key]
        [Column("alert_pending_key")]
        public int alert_pending_key { get; set; }
        
        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("last_updated")]
        public DateTime last_updated { get; set; }

        [Column("alert_message")]
        public string alert_message { get; set; }

        [Column("owner_read")]
        public bool owner_read { get; set; }

        [Column("owner_read_time")]
        public DateTime? owner_read_time { get; set; }

        [Column("alert_delete")]
        public bool alert_delete{ get; set; }

        [Column("icon_type")]
        public short icon_type { get; set; }

        [Column("icon_type_change")]
        public short icon_type_change { get; set; }
        
    }
}
