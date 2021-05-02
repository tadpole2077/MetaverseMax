using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public class DistrictDB
    {
        private readonly MetaverseMaxDbContext _context;

        public DistrictDB(MetaverseMaxDbContext _parentContext)
        {
            _context = _parentContext;
        }

        public DbSet<District> Districts { get; set; }   // Links to a specific table in DB

        public IEnumerable<District> DistrictGetAll(bool isOpened)
        {
            List<District> districtList;

            districtList = _context.district.OrderBy(x => x.district_id).ToList();

            return districtList.ToArray();
        }

        public IEnumerable<int> DistrictId_GetList()
        {
            List<int> districtList;

            districtList = _context.district.OrderBy(x => x.district_id)
                  .Select(r => r.district_id).Distinct().ToList();
            
            return districtList.ToArray();
        }

        public District DistrictGet(int districtId)
        {
            District matchedDistrict = new();

            try
            {
                matchedDistrict = _context.district.Where(x => x.district_id == districtId)
                    .OrderByDescending(x => x.update_instance)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictDB::DistrictGet() : Error Getting district_id = ", districtId));
                _context.LogEvent(log);
            }

            return matchedDistrict;
        }

        public DistrictContent DistrictContentGet(int districtId)
        {
            DistrictContent matchedDistrictContent = new();

            try
            {
                matchedDistrictContent = _context.districtContent.Where(x => x.district_id == districtId).FirstOrDefault();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictDB::DistrictContentGet() : Error Getting district_id = ", districtId));
                _context.LogEvent(log);
            }

            return matchedDistrictContent;
        }

        public int DistrictUpdate( District district )
        {
            int result;
            try
            {

                SqlParameter districtParameter = new SqlParameter("@district_id", district.district_id);
                SqlParameter updateInstance = new SqlParameter
                {
                    ParameterName = "@update_instance",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output,
                };

                //exec sproc to add set of owner summary rows matching instanct of district.
                result = _context.Database.ExecuteSqlRaw("EXEC @update_instance = dbo.sp_update_district_summary @district_id", new[] { districtParameter, updateInstance });

                //Use the new update_instance returned from summary sproc and assign to new district row.
                district.update_instance = (int)updateInstance.Value;
                district.last_update = DateTime.Now;
                _context.district.Add(district);
                _context.SaveChanges();

                //Update summary totals for district
                updateInstance.Direction = System.Data.ParameterDirection.Input;    // reusing parameter but now using as input - not output.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_update_district_by_instance @district_id, @update_instance", new[] { districtParameter, updateInstance });
               

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                 _context.LogEvent(String.Concat("DistrictDB::DistrictUpdate() : Error updating district_id = ", district.district_id));
                _context.LogEvent(log);
            }

            return 0;
        }

        public int DistrictAddOrUpdate(District district)
        {
            District matchedDistrict;
            try
            {
                matchedDistrict = DistrictGet(district.district_id);

                if (matchedDistrict != null && matchedDistrict.district_key > 0)
                {
                    matchedDistrict.last_update = DateTime.Now;
                    matchedDistrict.owner_matic = district.owner_matic;
                    matchedDistrict.owner_name = district.owner_name;
                    matchedDistrict.owner_avatar_id = district.owner_avatar_id;
                    matchedDistrict.plots_claimed = district.plots_claimed;
                    matchedDistrict.land_count = district.land_count;
                    matchedDistrict.building_count = district.building_count;

                    _context.district.Update(matchedDistrict);
                }
                else
                {
                    district.last_update = DateTime.Now;
                    _context.district.Add(district);
                }

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictDB::DistrictUpdate() : Error updating district_id = ", district.district_id));
                _context.LogEvent(log);
            }

            return 0;
        }
    }
}
