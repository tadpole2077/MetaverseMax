using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("Plot")]
    public class Plot
    {
        [Key]
        [Column("plot_id")]
        public int plot_id { get; set; }

        [Column("cell_id")]
        public int cell_id { get; set; }

        [Column("district_id")]
        public int district_id { get; set; }

        [Column("pos_x")]
        public int pos_x { get; set; }

        [Column("pos_y")]
        public int pos_y { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal current_price { get; set; }

        [Column("last_updated")]
        public DateTime? last_updated {get; set;}

        [Column("notes")]
        public string notes { get; set; }
        
        [Column("unclaimed_plot")]
        public bool unclaimed_plot { get; set; }

        [Column("owner_nickname")]
        public string owner_nickname { get; set; }

        [Column("owner_matic")]
        public string owner_matic { get; set; }

        [Column("owner_avatar_id")]
        public int owner_avatar_id { get; set; }
        
        [Column("land_type")]
        public int land_type { get; set; }

        [Column("resources")]
        public int resources { get; set; }
        
        [Column("building_id")]
        public int building_id { get; set; }
        
        [Column("building_level")]
        public int building_level { get; set; }
        
        [Column("building_type_id")]
        public int building_type_id { get; set; }
        
        [Column("token_id")]
        public int token_id { get; set; }

        [Column("on_sale")]
        public bool on_sale { get; set; }

        [Column("abundance")]
        public int? abundance { get; set; }
    }
}
