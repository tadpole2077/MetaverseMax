using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("DistrictPerk")]
    public class DistrictPerk
    {
        [Key]
        [Column("perk_key")]
        public int perk_key { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("update_instance")]
        public int update_instance { get; set; }

        [Column("perk_id")]
        public int perk_id { get; set; }

        [Column("perk_level")]
        public int perk_level { get; set; }
    }

}