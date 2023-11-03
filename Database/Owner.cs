using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    //[Keyless]       // Available from EF Core 5 
    [Table("Owner")]
    public class Owner    
    {
        [Key]
        [Column("owner_matic_key")]
        public string? owner_matic_key { get; set; }

        [Column("tool_active")]
        public bool tool_active { get; set; }

        [Column("public_key")]
        public string? public_key { get; set; }

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

        [Column("pet_count")]
        public int? pet_count { get; set; }

        [Column("citizen_count")]
        public int? citizen_count { get; set; }

        [Column("pro_access_expiry")]
        public DateTime? pro_access_expiry { get; set; }

        [Column("pro_access_renew_code")]
        public string? pro_access_renew_code { get; set; }

        [Column("dark_mode")]
        public bool dark_mode { get; set; }

        [Column("alert_activated")]
        public bool alert_activated { get; set; }

        [Column("created_date")]
        public DateTime created_date { get; set; }

        // Account can update names - this is the last updated name that is not blank.
        [NotMapped]
        [Column("owner_name")]
        public string? owner_name { get; set; }

        [NotMapped]
        [Column("avatar_id")]
        public int? avatar_id { get; set; }


    }


    [Table("OwnerEXT")]
    public class OwnerEXT
    {
        [Key]
        [Column("owner_matic_key")]
        public string? owner_matic_key { get; set; }

        [Column("tool_active")]
        public bool tool_active { get; set; }

        [Column("public_key")]
        public string? public_key { get; set; }

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

        [Column("pet_count")]
        public int? pet_count { get; set; }

        [Column("citizen_count")]
        public int? citizen_count { get; set; }

        [Column("pro_access_expiry")]
        public DateTime? pro_access_expiry { get; set; }

        [Column("pro_access_renew_code")]
        public string? pro_access_renew_code { get; set; }

        // Account can update names - this is the last updated name that is not blank.
        [Column("owner_name")]
        public string? owner_name { get; set; }

        [Column("avatar_id")]
        public int? avatar_id { get; set; }

        [Column("dark_mode")]
        public bool dark_mode { get; set; }

        [Column("alert_activated")]
        public bool alert_activated { get; set; }
    }
}
