using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class BuildingTypeIPDB : DatabaseBase
    {
        public BuildingTypeIPDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }


        public IEnumerable<BuildingTypeIP> BuildingTypeGet(int buildingType, int buildingLevel)
        {
            List<BuildingTypeIP> buildingList = new();

            try
            {
                buildingList = _context.buildingTypeIP.FromSqlInterpolated($"exec sp_building_type_IP_get { buildingType }, { buildingLevel }").AsNoTracking().ToList();
                for(int index=0; index < buildingList.Count; index++)
                {
                    buildingList[index].eval_ip_bonus = buildingList[index].app_123_bonus +
                        (buildingList[index].is_perk_activated ? buildingList[index].app_4_bonus + buildingList[index].app_5_bonus : 0);
                }
                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictDB::DistrictGetAll_Latest() : Error executing sproc sp_get_all_district_latest"));
            }

            return buildingList.ToArray();
        }
    }
}
