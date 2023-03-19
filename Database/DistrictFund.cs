using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // DECIMAL - by default an 18,2 decimal is used (2 percision), MCP use a 6,6 (6 decimal places to support ETH) -  this requires a 12,6 override in C#
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("DistrictFund")]
    public class DistrictFund
    {
        [Key]
        [Column("fund_key")]
        public int fund_key { get; set; }

        [Column("update")]
        public DateTime update { get; set; }

        [Column("balance")]
        public decimal balance { get; set; }

        [Column("distribution")]
        public decimal distribution { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

    }
}
