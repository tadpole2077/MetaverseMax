using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("DistrictTaxChange")]
    public class DistrictTaxChange
    {
        [Key]
        [Column("change_key")]
        public int change_key { get; set; }

        [Column("change_date")]
        public DateTime change_date { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("tax_type")]
        public string tax_type { get; set; }

        [Column("tax")]
        public int tax { get; set; }

        [Column("change_desc")]
        public string change_desc { get; set; }

        [Column("change_owner")]
        public string change_owner { get; set; }

        [Column("change_value")]
        public int change_value { get; set; }

    }
}
