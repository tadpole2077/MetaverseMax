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
    [Table("Sync")]
    public class Sync
    {
        [Key]
        [Column("sync_key")]
        public int plot_id { get; set; }

        [Column("name")]
        public string detail { get; set; }

        [Column("world")]
        public int world { get; set; }

        [Column("run_sequence_pos")]
        public int run_sequence_pos { get; set; }

        [Column("active")]
        public bool active { get; set; }

    }
}

