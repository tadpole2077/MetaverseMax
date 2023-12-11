using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetaverseMax.Database
{
    // Using Entity Framework Attributes to define tables (other method is API Fluent)
    [Table("PersonalSign")]
    public class PersonalSign
    {
        [Key]
        [Column("sign_key")]
        public int sign_id { get; set; }

        [Column("matic_key")]
        public string matic_key { get; set; }

        [Column("encode_byte")]
        public string encode_byte { get; set; }

        [Column("signed_key")]
        public string signed_key { get; set; }

        [Column("salt")]
        public string salt { get; set; }

        [Column("created")]
        public DateTime created { get; set; }

        [Column("amount")]
        public string amount { get; set; }

    }
}
