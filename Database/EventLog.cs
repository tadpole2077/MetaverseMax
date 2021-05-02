using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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
