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
                // do not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). min overhead on retriving and use of entity                
                buildingList = _context.buildingTypeIP.FromSqlInterpolated($"exec sp_building_type_IP_get { buildingType }, { buildingLevel }").AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("BuildingTypeIPDB::BuildingTypeGet() : Error executing sproc sp_building_type_IP_get - buildingType: ", buildingType.ToString(), " , buildingLevel: ", buildingLevel.ToString()));
            }

            return buildingList.ToArray();
        }
    }
}
