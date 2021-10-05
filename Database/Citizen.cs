using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Citizen")]
    public class Citizen
    {
        [Key]
        [Column("citizen_key")]
        public int citizen_key { get; set; }

        [Index(IsUnique = true)]
        [Column("token_id")]
        public int token_id { get; set; }

        [Column("name")]
        public string name { get; set; }

        [Column("generation")]
        public int generation { get; set; }

        [Column("breeding")]
        public int breeding { get; set; }

        [Column("sex")]
        public short? sex { get; set; }

        [Column("trait_agility")]
        public int trait_agility { get; set; }

        [Column("trait_intelligence")]
        public int trait_intelligence { get; set; }

        [Column("trait_charisma")]
        public int trait_charisma { get; set; }

        [Column("trait_endurance")]
        public int trait_endurance { get; set; }

        [Column("trait_luck")]
        public int trait_luck { get; set; }

        [Column("trait_strength")]
        public int trait_strength { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("max_stamina")]
        public int max_stamina { get; set; }

        [Column("efficiency_industry")]
        public double efficiency_industry { get; set; }

        [Column("efficiency_production")]
        public double efficiency_production { get; set; }

        [Column("efficiency_energy")]
        public double efficiency_energy { get; set; }

        [Column("efficiency_office")]
        public double efficiency_office { get; set; }

        [Column("efficiency_commercial")]
        public double efficiency_commercial { get; set; }

        [Column("efficiency_municipal")]
        public double efficiency_municipal { get; set; }

        [Column("create_date")]
        public DateTime create_date { get; set; }

    }
}
