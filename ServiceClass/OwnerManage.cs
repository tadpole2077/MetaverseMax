using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    
    public class OwnerManage
    {
        private readonly MetaverseMaxDbContext _context;
        private static Dictionary<string, string> ownersList = new();
        
        public OwnerManage(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;

            GetOwners(); // Check if static dictionary has been loaded

        }

        private int GetOwners()
        {                        
            if (ownersList.Count == 0)
            {
                OwnerDB ownerDB = new OwnerDB(_context);
                ownersList = ownerDB.GetOwners(WORLD_TYPE.TRON);
            }

            return 0;
        }

        public OwnerAccount FindOwnerByMatic(string maticKey, string tronKey)
        {
            OwnerAccount ownerAccount = new();

            if (ownersList.TryGetValue(maticKey, out tronKey))
            {

                // Matic found, but no Tron key
                if (tronKey == string.Empty)
                {
                    OwnerDB ownerDB = new OwnerDB(_context);
                    ownerDB.UpdateOwner(maticKey, tronKey);

                    ownersList[maticKey] = tronKey;
                }

                ownerAccount.matic_key = maticKey;
                ownerAccount.tron_key = tronKey;
            }
            else
            {
                ownerAccount.matic_key = "Not Found";
                ownerAccount.checked_matic_key = maticKey;
            }

            return ownerAccount;
        }
    }

    
}
