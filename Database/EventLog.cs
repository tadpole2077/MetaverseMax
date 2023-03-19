using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("EventLog")]
    public class EventLog
    {
        [Key]
        [Column("log_id")]
        public int plot_id { get; set; }

        [Column("detail")]
        public string detail { get; set; }

        [Column("recorded_time")]
        public DateTime recorded_time { get; set; }

    }
}
