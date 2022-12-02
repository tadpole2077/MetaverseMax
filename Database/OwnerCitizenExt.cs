using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("OwnerCitizenExt")]
    public class OwnerCitizenExt : CitizenTrait
    {
        [Key]
        [Column("link_key")]
        public int link_key { get; set; }

        [Column("pet_token_id")]
        public int pet_token_id { get; set; }

        [Column("pet_bonus_id")]
        public int? pet_bonus_id { get; set; }

        [Column("pet_bonus_level")]
        public int? pet_bonus_level { get; set; }

        
        [Column("district_id")]
        public int? district_id { get; set; }

        [Column("land_token_id")]
        public int? land_token_id { get; set; }

        [Column("building_type_id")]
        public int? building_type_id { get; set; }

        [Column("building_id")]
        public int? building_id { get; set; }

        [Column("building_level")]
        public int? building_level { get; set; }

        [Column("pos_x")]
        public int? pos_x { get; set; }

        [Column("pos_y")]
        public int? pos_y { get; set; }



        [Column("token_id")]
        public int token_id { get; set; }

        [Column("name")]
        public string name { get; set; }

        [Column("generation")]
        public int generation { get; set; }

        [Column("sex")]
        public short? sex { get; set; }

        [Column("breeding")]
        public int breeding { get; set; }       

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("current_price", TypeName = "decimal(18, 2)")]
        public decimal? current_price { get; set; }

        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("max_stamina")]
        public int max_stamina { get; set; }        

        [Column("create_date")]
        public DateTime create_date { get; set; }

        [Column("valid_to_date")]
        public DateTime? valid_to_date { get; set; }

        [Column("link_date")]
        public DateTime link_date { get; set; }

        [Column("refreshed_last")]
        public DateTime? refreshed_last { get; set; }        

    }
}
