using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


#nullable enable
namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    //[Keyless]       // Available from EF Core 5 
    [Table("OwnerMaterial")]
    public class OwnerMaterial
    {
        [Key]
        [Column("om_key")]
        public int om_key { get; set; }

        [Column("owner_matic_key")]
        public string? owner_matic_key { get; set; }

        [Column("wood")]
        public int wood { get; set; }

        [Column("sand")]
        public int sand { get; set; }

        [Column("stone")]
        public int stone { get; set; }

        [Column("metal")]
        public int metal { get; set; }

        [Column("brick")]
        public int brick { get; set; }

        [Column("glass")]
        public int glass { get; set; }

        [Column("water")]
        public int water { get; set; }

        [Column("energy")]
        public int energy { get; set; }

        [Column("steel")]
        public int steel { get; set; }

        [Column("concrete")]
        public int concrete { get; set; }

        [Column("plastic")]
        public int plastic { get; set; }

        [Column("glue")]
        public int glue { get; set; }

        [Column("mixes")]
        public int mixes { get; set; }

        [Column("composites")]
        public int composites { get; set; }

        [Column("paper")]
        public int paper { get; set; }

        [Column("last_updated")]
        public DateTime? last_updated { get; set; }

    }
}
