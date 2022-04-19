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
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == ownerMatickey).FirstOrDefault();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerDB::GetOwner() : Error getting Owner with MaticKey = ", ownerMatickey));
                    _context.LogEvent(log);
                }
            }

            return owner;
        }

        public Dictionary<string, string> GetOwners(WORLD_TYPE world)
        {
            Dictionary<string, string> ownersList = new();
            try
            {
                ownersList = _context.owner.Where(o => o.active_tron == true)
                    .ToDictionary(o => o.owner_matic_key, o => o.owner_tron_key);
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerDB::GetOwners() : Error all owners loading into Dictionary"));
                    _context.LogEvent(log);
                }
            }

            return ownersList;
        }

        public bool UpdateOwner(string maticKey, string tronKey){

            Owner owner = new();
            try
            {
                owner = _context.owner.Where(o => o.owner_matic_key == maticKey && o.active_tron).FirstOrDefault();
                owner.owner_tron_key = tronKey;
                owner.last_use = DateTime.Now;

                // Slow down Nightly job process when 1+ user is active via (a)save on each db update (b) increase cycle wait interval to 1 second : avoids user db timeouts (such as opening large Cit collection).
                if (SyncWorld.saveDBOverride == false)
                {
                    _ = ResetDataSync();
                }
                SyncWorld.saveDBOverride = true;
                SyncWorld.jobInterval = 1000;

                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerDB::UpdateOwner() : Error updating owner with Matic Key - ", maticKey));
                    _context.LogEvent(log);
                }
            }

            return true; 
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
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("OwnerDB::SyncOwner() : Error sync owners after nightly sync "));
                    _context.LogEvent(log);
                }
            }

            return returnCode;
        }

        // 5 minute reset of Nightly Data sync job to default settings  (.Net 4.5+ rec solution)
        public async Task ResetDataSync()
        {
            var timeoutInMilliseconds = TimeSpan.FromMinutes(5);

            await Task.Delay(timeoutInMilliseconds);

            SyncWorld.SyncPlotData_Reset();

        }

    }
}
