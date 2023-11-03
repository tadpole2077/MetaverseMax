using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("MissionActive")]
    public class MissionActive
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
        public DateTime last_updated { get; set; }

        [Column("available")]
        public bool available { get; set; }

        [Column("balance", TypeName = "decimal(10, 4)")]
        public decimal balance { get; set; }


        [Column("pos_x")]
        public int pos_x { get; set; }

        [Column("pos_y")]
        public int pos_y { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("building_id")]
        public int building_id { get; set; }

        [Column("building_level")]
        public int building_level { get; set; }

        [Column("building_type_id")]
        public int building_type_id { get; set; }
    }
}
