using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("SyncHistory")]
    public class SyncHistory
    {
        [Key]
        [Column("history_key")]
        public int history_key { get; set; }

        [Column("type")]
        public string type { get; set; }

        [Column("sync_start")]
        public DateTime sync_start { get; set; }

        [Column("sync_end")]
        public DateTime sync_end { get; set; }

        [Column("sync_duration")]
        public string sync_duration { get; set; }

    }
}
