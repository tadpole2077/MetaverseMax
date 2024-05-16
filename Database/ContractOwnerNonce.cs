using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetaverseMax.Database
{
    public class ContractOwnerNonce
    {
        [Key]
        [Column("account_id")]
        public int account_id { get; set; }

        [Column("public_key")]
        public string public_key { get; set; }

        [Column("chain_id")]
        public int chain_id { get; set; }

        [Column("last_nonce")]
        public int last_nonce { get; set; }
    }
}
