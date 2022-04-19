using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class DistrictPerkDB : DatabaseBase
    {
        public DistrictPerkDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }        

        public IEnumerable<DistrictPerk> PerkGetAll(int districtId, int updateInstance)
        {
            List<DistrictPerk> districtPerkList = null;

            try
            {
                districtPerkList = _context.districtPerk.Where(x => x.district_id == districtId && x.update_instance == updateInstance).ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictPerkDB.PerkGetAll() : Error Getting perks for district : ", districtId));
            }

            return districtPerkList.ToArray();
        }

        public List<DistrictPerk> PerkGetAll_ByPerkType(int perkId)
        {
            List<DistrictPerk> districtPerkList = null;

            try
            {
                //(int)DISTRICT_PERKS.EXTRA_SLOT_APPLIANCE_ALL_BUILDINGS
                districtPerkList = _context.districtPerk.FromSqlInterpolated($"[sp_district_perk_by_type_get] {perkId}").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictPerkDB.PerkGetAll_ByPerkType() : Error Getting perks with key : ", perkId));
            }

            return districtPerkList;
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
                logException(ex, String.Concat("DistrictPerkDB :: Save() : Error saving District Perks "));
            }

            return 0;
        }
    }
}
