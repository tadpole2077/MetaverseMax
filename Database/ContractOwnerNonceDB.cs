using MetaverseMax.BaseClass;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class ContractOwnerNonceDB : DatabaseBase
    {
        private new readonly MetaverseMaxDbContext_UNI _context;

        public ContractOwnerNonceDB(MetaverseMaxDbContext_UNI _parentContext) : base()
        {
            _context = _parentContext;
        }

        public int GetNextNonce(string contractOwnerPublicKey, NETWORK chainId)
        {
            int result;
            int nextNonce = 0;
            try
            {
                SqlParameter publicKeyParameter = new("@public_key", contractOwnerPublicKey);
                SqlParameter chainParameter = new("@chain_id", (int)chainId);

                SqlParameter nextNonceParameter = new()
                {
                    ParameterName = "@next_nonce",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,

                };

                result = _context.Database.ExecuteSqlRaw("EXEC @next_nonce = dbo.sp_nonce_increment @public_key, @chain_id",
                    new[] { nextNonceParameter, publicKeyParameter, chainParameter});

                nextNonce = (int)nextNonceParameter.Value;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("ContractOwnerNonceDB::GetNextNonce() : Error getting next nonce number for Contract Owner: ", contractOwnerPublicKey, " on chain: ", chainId));
            }

            return nextNonce;
        }
    }
   
}
