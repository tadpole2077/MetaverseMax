using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("OwnerCitizen")]
    public class OwnerCitizen
    {
        [Key]
        [Column("link_key")]
        public int link_key { get; set; }

        [Column("link_date")]
        public DateTime link_date { get; set; }

        [Column("owner_matic_key")]
        public string owner_matic_key { get; set; }

        [Column("land_token_id")]
        public int land_token_id { get; set; }

        [Column("citizen_token_id")]
        public int citizen_token_id { get; set; }

        [Column("pet_token_id")]
        public int pet_token_id { get; set; }

    }
}
