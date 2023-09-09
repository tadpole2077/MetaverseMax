using Microsoft.EntityFrameworkCore;
using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Database
{
    public class OwnerDB : DatabaseBase
    {
        public OwnerDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public Owner GetOwner(string ownerMatickey)
        {
            Owner owner = new();

            List<OwnerName> ownerNameList = new();
            OwnerName ownerName;
            try
            {
                ownerMatickey = ownerMatickey.ToLower();
                owner = _context.owner.Where(o => o.owner_matic_key == ownerMatickey).FirstOrDefault();
                ownerNameList = _context.ownerName.Where(o => o.owner_matic_key == ownerMatickey).ToList();

                // CHECK - if owner does not exist (potentially old transaction such as an offer - owner sold all plots before start of metaverseMax first sync).
                if (ownerNameList.Count() == 0 || owner == null)
                {
                    owner = new()
                    {
                        owner_name = ""
                    };
                }
                else
                {
                    // Get latest name in use for this account that is not empty or null
                    ownerName = ownerNameList.Where(o => string.IsNullOrEmpty(o.owner_name) == false)
                                                         .OrderByDescending(o => o.created_date)
                                                         .FirstOrDefault();
                    owner.owner_name = ownerName == null ? "" : ownerName.owner_name;

                    // Get latest avatar icon used for this account that is not blank.  Note account may have an avatar but blank name.
                    ownerName = ownerNameList.Where(o => o.avatar_id.HasValue && o.avatar_id != 0)
                                                         .OrderByDescending(o => o.created_date)
                                                         .FirstOrDefault();
                    owner.avatar_id = ownerName == null ? 0 : ownerName.avatar_id;
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::GetOwner() : Error getting Owner with MaticKey = ", ownerMatickey));
            }

            return owner;
        }

        public RETURN_CODE GetOwners(ref Dictionary<string, OwnerAccount> ownerList)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                // NOTE - each Owner account (owner_matic_key) 1 or more OwnerName records (only one will be the latest version).
                // Using string interpolation syntax to pull in parameters
                List<OwnerEXT> ownerDBList = _context.ownerEXT.FromSqlInterpolated($"sp_owner_get_all").AsNoTracking().ToList();


                ownerList = ownerDBList.ToDictionary(
                        o => o.owner_matic_key,
                        o => new OwnerAccount()
                        {
                            matic_key = o.owner_matic_key,
                            public_key = o.public_key,
                            name = o.owner_name,
                            avatar_id = o.avatar_id ?? 0,
                            dark_mode = o.dark_mode,
                            pro_tools_enabled = (o.pro_access_expiry ?? DateTime.UtcNow) > DateTime.UtcNow ? true : false,
                            pro_expiry_days = GetExpiryDays(o.pro_access_expiry, (o.pro_access_expiry ?? DateTime.UtcNow) > DateTime.UtcNow ? true : false),
                            alert_activated = o.alert_activated,
                        }
                        );

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::GetOwners() : Error loading all owners into Dictionary"));
                returnCode = RETURN_CODE.ERROR;
            }

            return returnCode;
        }

        private int GetExpiryDays(DateTime? pro_access_expiry, bool proToolsEnabled)
        {
            int proExpiryDays = 0;

            DateTime expiry = (pro_access_expiry ?? DateTime.UtcNow);
            // Handler for expiry next year or current year.
            if (proToolsEnabled && expiry.Year >= DateTime.UtcNow.Year)
            {
                if (DateTime.UtcNow.Year < expiry.Year)
                {
                    proExpiryDays = (365 - DateTime.UtcNow.DayOfYear) + expiry.DayOfYear;
                }
                else
                {
                    proExpiryDays = (pro_access_expiry ?? DateTime.UtcNow).DayOfYear - DateTime.UtcNow.DayOfYear;
                }
            }

            return proExpiryDays;
        }
        public RETURN_CODE UpdateOwnerDarkMode(string ownerMaticKey, bool darkMode)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            Owner owner = null;
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == ownerMaticKey).FirstOrDefault();

                if (owner != null)
                {
                    owner.dark_mode = darkMode;

                    _context.SaveChanges();
                    returnCode = RETURN_CODE.SUCCESS;
                }                
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::UpdateOwnerDarkMode() : Error updating owner with matic Key - ", ownerMaticKey));
            }


            return returnCode;
        }
        
        public RETURN_CODE UpdateOwnerAlertActivated(string ownerMaticKey, bool alertActivated)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            Owner owner = null;
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == ownerMaticKey).FirstOrDefault();

                if (owner != null)
                {
                    owner.alert_activated = alertActivated;

                    _context.SaveChanges();
                    returnCode = RETURN_CODE.SUCCESS;
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::UpdateOwnerAlertActivated() : Error updating owner with matic Key - ", ownerMaticKey));
            }


            return returnCode;
        }

        public Owner UpdateOwner(string maticKey, string publicKey, OwnerAccount ownerAccount)
        {

            Owner owner = null;
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == maticKey).FirstOrDefault();

                if (owner == null)
                {
                    // TEMP CODE - to trace while a few owners are identified as found in local sore when no matching account in db.
                    _context.LogEvent(String.Concat("OWNER ISSUE: ", maticKey, " not found in Owner db. But unexpected attempting to update it within code."));
                    if (ownerAccount != null)
                    {
                        _context.LogEvent(String.Concat("OWNER ISSUE: ", "The local store of owners returns a matching account with name: ", ownerAccount.name, " avatar id:", ownerAccount.avatar_id, " matic_key:", ownerAccount.matic_key));
                    }

                }
                else
                {
                    if (owner.public_key == string.Empty)
                    {
                        owner.public_key = publicKey;
                    }
                    owner.last_use = DateTime.Now;
                    owner.tool_active = true;

                    // Slow down Nightly job process when 1+ user is active via (a)save on each db update (b) increase cycle wait interval to 1 second : avoids user db timeouts (such as opening large Cit collection).
                    if (SyncWorld.syncInProgress == true && SyncWorld.saveDBOverride == false)
                    {
                        _ = ResetDataSync(_context);            // Allow aync to process in separate thread - 5 minute slowdown on data sync.
                    }

                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::UpdateOwner() : Error updating owner with Matic Key - ", maticKey));
            }

            return owner;
        }

        public int SyncOwner()
        {
            int result = 0, returnCode = 0;
            try
            {
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_owner_sync");

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::SyncOwner() : Error sync owners after nightly sync "));
            }

            return returnCode;
        }

        // 5 minute reset of Nightly Data sync job to default settings  (.Net 4.5+ rec solution)
        public async Task ResetDataSync(MetaverseMaxDbContext passedContext)
        {
            SyncWorld.saveDBOverride = true;
            var oldInterval = SyncWorld.jobInterval;
            SyncWorld.jobInterval = 1000;

            if (passedContext != null)
            {
                ServicePerfDB servicePerfDB = new(passedContext);
                servicePerfDB.AddServiceEntry("ResetDataSync() 5min period - 1 second interval calls", DateTime.Now, 5000, 0, "");
            }

            var timeoutInMilliseconds = TimeSpan.FromMinutes(5);
            await Task.Delay(timeoutInMilliseconds);

            SyncWorld.jobInterval = oldInterval;
            SyncWorld.saveDBOverride = false;

        }

    }
}
