using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("OwnerOffer")]
    public class OwnerOffer
    {
        [Key]
        [Column("offer_id")]
        public int offer_id { get; set; }

        /*[Column("mcp_offer_id")]
        public int mcp_offer_id { get; set; }
        */
        [Column("buyer_matic_key")]
        public string buyer_matic_key { get; set; }

        [Column("buyer_offer")]
        public decimal buyer_offer { get; set; }

        [Column("token_owner_matic_key")]
        public string token_owner_matic_key { get; set; }

        [Column("token_id")]
        public int token_id { get; set; }

        [Column("token_type")]
        public int token_type { get; set; }

        [Column("offer_date")]
        public DateTime? offer_date { get; set; }

        [Column("active")]
        public bool active { get; set; }

        [Column("plot_x")]
        public int plot_x { get; set; }

        [Column("plot_y")]
        public int plot_y { get; set; }

        [Column("plot_district")]
        public int plot_district { get; set; }

    }
}
