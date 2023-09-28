using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Mission")]
    public class Mission
    {
        [Key]
        [Column("token_id")]
        public int token_id { get; set; }

        [Column("completed")]
        public int completed { get; set; }

        [Column("max")]
        public int max { get; set; }

        [Column("reward", TypeName = "decimal(16, 4)")]
        public decimal reward { get; set; }

        [Column("reward_owner", TypeName = "decimal(16, 4)")]
        public decimal reward_owner { get; set; }

        [Column("last_updated")]
        public DateTime? last_updated { get; set; }
    }
}
