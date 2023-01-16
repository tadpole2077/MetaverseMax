using MetaverseMax.ServiceClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class SyncDB : DatabaseBase
    {

        public SyncDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }

        public List<Sync> Get(bool active)
        {
            List<Sync> syncList = new();

            try
            {
                syncList = _context.sync.Where(r => r.active == active)
                                      .OrderBy(r => r.run_sequence_pos)
                                      .ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("SyncDB.Get() : Error gettings sync jobs "));
            }

            return syncList;
        }
    }
}
