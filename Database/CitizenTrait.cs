using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    public class CitizenTrait
    {
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


        [NotMapped]
        [Column("trait_agility_pet_bonus")]
        public int trait_agility_pet_bonus { get; set; }

        [NotMapped]
        [Column("trait_intelligence_pet_bonus")]
        public int trait_intelligence_pet_bonus { get; set; }

        [NotMapped]
        [Column("trait_charisma_pet_bonus")]
        public int trait_charisma_pet_bonus { get; set; }

        [NotMapped]
        [Column("trait_endurance_pet_bonus")]
        public int trait_endurance_pet_bonus { get; set; }

        [NotMapped]
        [Column("trait_luck_pet_bonus")]
        public int trait_luck_pet_bonus { get; set; }

        [NotMapped]
        [Column("trait_strength_pet_bonus")]
        public int trait_strength_pet_bonus { get; set; }

        [Column("efficiency_industry")]
        public double efficiency_industry { get; set; }

        [Column("efficiency_production")]
        public double efficiency_production { get; set; }

        [Column("efficiency_energy_water")]
        public double efficiency_energy_water { get; set; }

        [Column("efficiency_energy_electric")]
        public double efficiency_energy_electric { get; set; }

        [Column("efficiency_office")]
        public double efficiency_office { get; set; }

        [Column("efficiency_commercial")]
        public double efficiency_commercial { get; set; }

        [Column("efficiency_municipal")]
        public double efficiency_municipal { get; set; }
    }
}
