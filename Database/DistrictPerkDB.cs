using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class DistrictPerkDB
    {
        private readonly MetaverseMaxDbContext _context;

        public DistrictPerkDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }        

        public IEnumerable<DistrictPerk> PerkGetAll(int districtId, int updateInstance)
        {
            List<DistrictPerk> districtPerkList;

            districtPerkList = _context.districtPerk.Where(x => x.district_id == districtId && x.update_instance == updateInstance).ToList();                

            return districtPerkList.ToArray();
        }

        public int Save(List<DistrictPerk> districtPerkList)
        {            
            try
            {
                for(int index = 0; index < districtPerkList.Count; index++)
                {                    
                    _context.districtPerk.Add(districtPerkList[index]);                 
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictPerkDB :: Save() : Error saving District Perks "));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }
    }
}
