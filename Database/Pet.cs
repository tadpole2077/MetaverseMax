using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Pet")]
    public class Pet
    {
        [Key]
        [Column("pet_key")]
        public int pet_key { get; set; }

        [Column("token_id")]
        public int token_id { get; set; }

        [Column("bonus_id")]
        public int bonus_id { get; set; }

        [Column("bonus_level")]
        public int bonus_level { get; set; }

        [Column("pet_look")]
        public int pet_look { get; set; }

        [Column("token_owner_matic_key")]
        public string token_owner_matic_key { get; set; }

        [Column("last_update")]
        public DateTime? last_update { get; set; }
    }
}
