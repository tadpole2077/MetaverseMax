using MetaverseMax.BaseClass;
using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class TransactionManage : ServiceBase
    {
        public TransactionManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public int InsertLog(string hash, string fromWallet, string toWallet, int unitType, int unitAmount, decimal value, int status, int blockchain, int transactionType, int tokenId)
        {


            return 0;
        }
    }
}
