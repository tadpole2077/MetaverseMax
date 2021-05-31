using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("OwnerSummaryDistrict")]
    public class OwnerSummaryDistrict
    {
        [Key]
        [Column("summary_id")]
        public int summary_id { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("owner_nickname")]
        public string owner_nickname { get; set; }

        [Column("owner_avatar_id")]
        public int owner_avatar_id { get; set; }

        [Column("owned_plots")]
        public int owned_plots { get; set; }

        [Column("energy_count")]
        public int energy_count { get; set; }

        [Column("industry_count")]
        public int industry_count { get; set; }

        [Column("residential_count")]
        public int residential_count { get; set; }

        [Column("production_count")]
        public int production_count { get; set; }

        [Column("office_count")]
        public int office_count { get; set; }

        [Column("municipal_count")]
        public int municipal_count { get; set; }

        [Column("poi_count")]
        public int poi_count { get; set; }

        [Column("empty_count")]
        public int empty_count { get; set; }

        [Column("update_instance")]
        public int update_instance { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("commercial_count")]
        public int commercial_count { get; set; }

        [Column("new_owner")]
        public bool? new_owner { get; set; }

        [Column("new_owner_month")]
        public bool? new_owner_month { get; set; }
    }
}
