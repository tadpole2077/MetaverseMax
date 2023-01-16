using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;


namespace MetaverseMax.Database
{
    public class OwnerDB
    {
        private readonly MetaverseMaxDbContext _context;
        
        public OwnerDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
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

                    // Get latest avatar icon used for this account that is not blank.  Not account may have an avatar but blank name.
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

        public RETURN_CODE GetOwners(WORLD_TYPE world, ref Dictionary<string, string> ownersList)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            try
            {
                // NOTE - owner_matic_key may have mulitple records, as a different wallet is used to access the account.
                ownersList = _context.owner //.Where(x => x.player_key)
                    .ToDictionary(o => o.owner_matic_key, o => o.public_key);

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::GetOwners() : Error all owners loading into Dictionary"));
                returnCode = RETURN_CODE.ERROR;
            }

            return returnCode;
        }
        public IEnumerable<OwnerName> GetOwnerName(WORLD_TYPE world, ref Dictionary<string, string> ownersList)
        {
            List<OwnerName> OwnerNameList = new();

            try
            {
                // NOTE - owner_matic_key may have mulitple records, as a different wallet is used to access the account.
                //ownersList = _context.ownerName //.Where(x => x.player_key)
                //    .ToDictionary(o => o.owner_matic_key, o => o.public_key);

                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
                //OwnerNameList = _context.buildingTypeIP.FromSqlInterpolated($"exec sp_building_type_IP_get { buildingType }, { buildingLevel }").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("OwnerDB::GetOwnerName() : Error all owners loading into Dictionary"));
            }

            return OwnerNameList.ToArray();
        }

        public Owner UpdateOwner(string maticKey, string publicKey){

            Owner owner = null;
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == maticKey).FirstOrDefault();
                owner.public_key = publicKey;
                owner.last_use = DateTime.Now;
                owner.tool_active = true;

                // Slow down Nightly job process when 1+ user is active via (a)save on each db update (b) increase cycle wait interval to 1 second : avoids user db timeouts (such as opening large Cit collection).
                if (SyncWorld.syncInProgress == true && SyncWorld.saveDBOverride == false)
                {
                    _ = ResetDataSync(_context);            // Allow aync to process in separate thread - 5 minute slowdown on data sync.
                }             
                
                _context.SaveChanges();

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
            int result = 0, returnCode =0;
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
