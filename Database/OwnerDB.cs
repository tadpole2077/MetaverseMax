using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Database
{
    public class OwnerDB
    {
        private readonly MetaverseMaxDbContext _context;
        public OwnerDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public Dictionary<string, string> GetOwners(WORLD_TYPE world)
        {
            Dictionary<string, string> ownersList = new();
            try
            {
                ownersList = _context.owner.Where(o => o.active_tron == true).ToDictionary(o => o.owner_matic_key, o => o.owner_tron_key);
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
                owner = _context.owner.Where(o => o.owner_matic_key == maticKey).FirstOrDefault();
                owner.owner_tron_key = tronKey;
                owner.last_use = DateTime.Now;
                
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

    }
}
