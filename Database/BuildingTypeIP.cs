﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("BuilingTypeIP")]
    public class BuildingTypeIP
    {
        [Key]
        [Column("token_id")]
        public int token_id { get; set; }

        [NotMapped]
        [Column("position")]
        public int position { get; set; }

        [Column("current_influence_rank", TypeName = "decimal(6, 2)")]
        public decimal current_influence_rank { get; set; }

        [NotMapped]
        [Column("total_ip")]
        public int total_ip { get; set; }

        [NotMapped]
        [Column("ip_warning")]
        public string ip_warning { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("for_rent", TypeName = "decimal(16, 4)")]
        public decimal for_rent { get; set; }

        [Column("rented")]
        public bool rented { get; set; }

        [Column("current_price")]
        public decimal current_price { get; set; }

        [Column("influence_info")]
        public int influence_info { get; set; }

        [Column("influence")]
        public int influence { get; set; }

        [Column("influence_bonus")]
        public int influence_bonus { get; set; }

        [Column("app_4_bonus")]
        public int app_4_bonus { get; set; }

        [Column("app_5_bonus")]
        public int app_5_bonus { get; set; }

        [Column("app_123_bonus")]
        public int app_123_bonus { get; set; }

        [Column("production_poi_bonus", TypeName = "decimal(6, 2)")]
        public decimal production_poi_bonus { get; set; }

        [Column("owner_nickname")]
        public string owner_nickname { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("owner_avatar_id")]
        public int owner_avatar_id { get; set; }

        [Column("pos_x")]
        public int pos_x { get; set; }

        [Column("pos_y")]
        public int pos_y { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [NotMapped]
        [Column("produce_tax")]
        public int produce_tax { get; set; }

        [Column("building_id")]
        public int building_id { get; set; }

        [Column("building_abundance")]
        public int building_abundance { get; set; }

        [NotMapped]
        [Column("building_img")]
        public string building_img { get; set; }

        [Column("last_run_produce_date")]
        public DateTime? last_run_produce_date { get; set; }

        [Column("last_run_produce")]
        public int? last_run_produce { get; set; }

        [Column("last_run_produce_id")]
        public int last_run_produce_id { get; set; }

        [Column("last_run_produce_predict")]
        public bool last_run_produce_predict { get; set; }

        [Column("predict_produce")]
        public int? predict_produce { get; set; }        

        [NotMapped]
        [Column("predict_eval")]
        public bool predict_eval { get; set; }

        [NotMapped]
        [Column("predict_eval_result")]
        public int? predict_eval_result { get; set; }

        [NotMapped]
        [Column("active_building")]
        public bool active_building { get; set; }

        [Column("active_stamina")]
        public bool active_stamina { get; set; }

        [NotMapped]
        [Column("out_of_statina_alert")]
        public bool out_of_statina_alert { get; set; }

        [Column("condition")]
        public int condition { get; set; }

        [Column("citizen_count")]
        public int citizen_count { get; set; }

        [Column("last_action")]
        public DateTime? last_action { get; set; }
    }
}
