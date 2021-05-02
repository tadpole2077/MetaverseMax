using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("DistrictContent")]
    public class DistrictContent
    {
        [Key]
        [Column("district_id")]
        public int district_id { get; set; }

        [Column("district_name")]
        public string district_name { get; set; }

        [Column("last_update")]
        public DateTime last_update { get; set; }

        [Column("district_promotion")]
        public string district_promotion { get; set; }

        [Column("district_promotion_start")]
        public DateTime? district_promotion_start { get; set; }

        [Column("district_promotion_end")]
        public DateTime? district_promotion_end { get; set; }
    }
}
