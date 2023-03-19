using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

