using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("OwnerName")]
    public class OwnerName
    {
        [Key]
        [Column("owner_name_id")]
        public int owner_name_id { get; set; }

        [Column("owner_matic_key")]
        public string? owner_matic_key { get; set; }

        [Column("owner_name")]
        public string? owner_name { get; set; }

        [Column("discord_name")]
        public string? discord_name { get; set; }

        [Column("avatar_id")]
        public int? avatar_id { get; set; }

        [Column("created_date")]
        public DateTime? created_date { get; set; }
    }
}