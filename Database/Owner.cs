using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Owner")]
    public class Owner
    {
        [Key]
        [Column("owner_matic_key")]
        public string owner_matic_key { get; set; }

        [Column("owner_tron_key")]
        public string owner_tron_key { get; set; }

        [Column("owner_name")]
        public string? owner_name { get; set; }

        [Column("type")]
        public int? type { get; set; }

        [Column("player_key")]
        public int? player_key { get; set; }

        [Column("last_use")]
        public DateTime? last_use { get; set; }

        [Column("owner_lookup_count")]
        public int? owner_lookup_count { get; set; }

        [Column("district_lookup_count")]
        public int? district_lookup_count { get; set; }

        [Column("active_tron")]
        public bool active_tron { get; set; }
    }
}
