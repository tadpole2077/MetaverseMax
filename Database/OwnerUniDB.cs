using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;

namespace MetaverseMax.Database
{
    public class OwnerUniDB : DatabaseBase
    {
        MetaverseMaxDbContext_UNI _contextUni;
        public OwnerUniDB(MetaverseMaxDbContext_UNI _parentContext) : base()
        {
            _contextUni = _parentContext;
        }

        public OwnerUni GetOwner(string ownerMaticKey, WORLD_TYPE worldType)
        {
            OwnerUni ownerUni = null;            
            try
            {
                NETWORK networkKey = GetNetworkKey(worldType);
                MaticKeyLink maticKeyLink = _contextUni.maticKeyLink.Where(x => x.matic_key == ownerMaticKey && x.network_key == (int)networkKey).FirstOrDefault();

                if (maticKeyLink != null)
                {
                    ownerUni = _contextUni.ownerUni.Where(x => x.owner_uni_id == maticKeyLink.owner_uni_id).FirstOrDefault();                    
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::GetOwner() : Error getting owner: ", ownerMaticKey));
            }


            return ownerUni;
        }

        public RETURN_CODE PopulateOwnersDicFromDB(ref Dictionary<int, OwnerUni> ownerUniListStore)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                List<OwnerUni> ownerUniList = _contextUni.ownerUni.ToList();


                ownerUniListStore = ownerUniList.ToDictionary(
                    o => o.owner_uni_id,
                    o => o
                );

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::PopulateOwnersDicFromDB() : Error loading all owners into Dictionary"));
                returnCode = RETURN_CODE.ERROR;
            }

            return returnCode;
        }

        public OwnerUni NewOwner(string ownerMaticKey, NETWORK networkKey, WORLD_TYPE worldType)
        {
            OwnerUni owner = null;
            MaticKeyLink link;
            try
            {
                ownerMaticKey = ownerMaticKey.ToLower();

                if (_contextUni.maticKeyLink.Where(x => x.matic_key == ownerMaticKey && x.network_key == (int)networkKey).Any() == false)
                {

                    owner = _contextUni.ownerUni.Add(
                        new OwnerUni()
                        {
                            created_date = DateTime.UtcNow,
                            last_updated = DateTime.UtcNow,
                            balance = 0,
                            balance_visible = false,
                            linked_wallet_count = 1,
                            allow_link = false
                        }).Entity;

                    _contextUni.SaveChanges();

                    link = _contextUni.maticKeyLink.Add(
                        new MaticKeyLink()
                        {
                            owner_uni_id = owner.owner_uni_id,
                            matic_key = ownerMaticKey,
                            network_key = (int)networkKey,
                            linked_on = DateTime.UtcNow
                        }).Entity;

                    _contextUni.SaveChanges();

                    UpdateWorldOwner(owner.owner_uni_id, worldType, ownerMaticKey);
                }

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::NewOwner() : Error creating new owner record with matic Key - ", ownerMaticKey));
            }
            return owner;
        }

        public RETURN_CODE UpdateWorldOwner(int OwnerUniID, WORLD_TYPE worldType, string ownerMaticKey)
        {
            RETURN_CODE response;

            using (MetaverseMaxDbContext _context = new MetaverseMaxDbContext(worldType))
            {
                OwnerDB ownerDB = new(_context);
                response = ownerDB.UpdateOwner_UniID(ownerMaticKey, OwnerUniID);                    
            }
           
            return response;
        }


        public OwnerUni WalletCountChange(int change, int ownerUniID)
        {
            OwnerUni owner = null;
            try
            {

                owner = _contextUni.ownerUni.Where(x => x.owner_uni_id == ownerUniID).FirstOrDefault();
                owner.linked_wallet_count += change;
                owner.last_updated = DateTime.UtcNow;

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::WalletCountChange() : Error creating change linked wallet count for account - ", ownerUniID));
            }
            return owner;
        }

        public bool CheckLink(OwnerChange ownerChange, WORLD_TYPE worldType)
        {
            bool updated = false;
            try
            {
                NETWORK networkKey = GetNetworkKey(worldType);
                MaticKeyLink maticKeyLink = _contextUni.maticKeyLink.Where(x => x.matic_key == ownerChange.owner_matic_key && x.network_key == (int)networkKey).FirstOrDefault();

                // CHECK owner key pair [matic key + world] is not registed within Universe DB
                if (maticKeyLink == null)
                {
                    // Check if owner exists (matching matic key) used with other worlds. Then assign existing accountUni ID to new wallet link.
                    MaticKeyLink link = _contextUni.maticKeyLink.Where(x => x.matic_key == ownerChange.owner_matic_key).FirstOrDefault();

                    if (link != null)
                    {
                        OwnerUni ownerUni = _contextUni.ownerUni.Where(x => x.owner_uni_id == link.owner_uni_id).FirstOrDefault();

                        _contextUni.maticKeyLink.Add(
                            new MaticKeyLink()
                            {
                                owner_uni_id = link.owner_uni_id,
                                matic_key = link.matic_key,
                                network_key = (int)networkKey,
                                linked_on = DateTime.UtcNow
                            });

                        WalletCountChange(1, link.owner_uni_id);

                        _contextUni.SaveChanges();

                        // Update world.owner record with owner_uni_id
                        UpdateWorldOwner(ownerUni.owner_uni_id, worldType, ownerChange.owner_matic_key);
                    }
                    else
                    {
                        // New owner account - create new OwnerUni and link records.
                        NewOwner(ownerChange.owner_matic_key, networkKey, worldType);
                    }
                    updated = true;
                }                

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::CheckLink() : Error checking link for owner with matic Key - ", ownerChange.owner_matic_key));
            }

            return updated;
        }

        // Update balance this owner wallet is linked to.
        public decimal UpdateOwnerBalance(decimal amount, int ownerUniID)
        {
            OwnerUni ownerUni = null;
            decimal? oldBalance = 0;
            try
            {
                ownerUni = _contextUni.ownerUni.Where(x => x.owner_uni_id == ownerUniID).FirstOrDefault();
                if (ownerUni != null)
                {
                    oldBalance = ownerUni.balance ?? 0;

                    ownerUni.balance ??= 0;
                    ownerUni.balance += amount;
                }

                _contextUni.SaveWithRetry();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerUniDB::UpdateOwnerBalance() : Error updating balance for ownerUniID: ", ownerUniID, ", Old Balance: ", oldBalance, " ,change amount: ", amount));
            }


            return ownerUni.balance ?? 0;
        }

        public RETURN_CODE UpdateBalanceVisible(int ownerUniID, bool visible)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            OwnerUni ownerUni = null;
            try
            {
                ownerUni = _contextUni.ownerUni.Where(x => x.owner_uni_id == ownerUniID).FirstOrDefault();
                if (ownerUni != null)
                {                    
                    ownerUni.balance_visible = visible;

                    _contextUni.SaveChanges();
                    returnCode = RETURN_CODE.SUCCESS;
                }

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                returnCode = RETURN_CODE.ERROR;
                DBLogger dBLogger = new();
                dBLogger.logException(ex, String.Concat("OwnerDB::UpdateBalanceVisible() : Error updating owner with ownerUnitID - ", ownerUni));
            }

            return returnCode;
        }

        public static NETWORK GetNetworkKey(WORLD_TYPE worldType)
        {
            return worldType switch
            {
                WORLD_TYPE.TRON => NETWORK.TRON_ID,
                WORLD_TYPE.ETH => NETWORK.ETHEREUM_ID,
                WORLD_TYPE.BNB => NETWORK.BINANCE_ID,
                _ => NETWORK.BINANCE_ID
            };
        }
    }
}
