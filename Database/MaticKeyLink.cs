using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    //[Keyless]       // Available from EF Core 5 
    [Table("MaticKeyLink")]
    public class MaticKeyLink
    {
        [Key]
        [Column("matic_key_link")]
        public int matic_key_link { get; set; }
        
        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("network_key")]
        public int network_key { get; set; }

        [Column("owner_uni_id")]
        public int owner_uni_id { get; set; }
        
        [Column("linked_on")]
        public DateTime? linked_on { get; set; }

    }
}
