using MetaverseMax.ServiceClass;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaverseMax.Database
{
    public class DistrictDB : DatabaseBase
    {
        public DistrictDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public DbSet<District> district { get; set; }   // Links to a specific table in DB

        public IEnumerable<District> DistrictGetAll(bool isOpened)
        {
            List<District> districtList;

            districtList = _context.district.OrderBy(x => x.district_id).ToList();

            return districtList.ToArray();
        }

        public IEnumerable<int> DistrictId_GetList()
        {
            List<int> districtList;

            districtList = _context.district
                           .Select(r => r.district_id)
                           .Distinct()
                           .OrderBy(r => ((uint)r))
                           .ToList();

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
                logException(ex, String.Concat("DistrictDB::DistrictGet() : Error Getting district_id = ", districtId));
            }

            return matchedDistrict ?? new District();
        }

        //Get District snapshot from 1 month prior to extract prior district tax attributes
        public District DistrictGet_History1Mth(int districtId, DateTime districtLatestUpdate)
        {
            District matchedDistrict = new();

            try
            {
                matchedDistrict = _context.district.Where(x => x.district_id == districtId && x.last_update <= districtLatestUpdate.AddMonths(-1))
                    .OrderByDescending(x => x.update_instance)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictDB::DistrictGet_History1Mth() : Error Getting district_id = ", districtId));
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
                logException(ex, String.Concat("DistrictDB::DistrictContentGet() : Error Getting district_id = ", districtId));
            }

            return matchedDistrictContent;
        }

        public IEnumerable<District> DistrictGetAll_Latest()
        {
            List<District> districtList = new();
            try
            {
                //IEnumerable<District> test = 
                districtList = _context.district.FromSqlInterpolated($"sp_get_all_district_latest").AsNoTracking().ToList();
                // AsNoTracking() will not track changes to the entity data, used for read-only scenarios, can not use SaveChanges(). likely results in min overhead on retriving and use of entity

                var x = districtList.Count;

                // Call sproc to get list of latest districts with matching UpdateInstance keys needed to pull the correct set of owner summary records.
                //districtList = _context.district.FromSqlInterpolated<District>($"EXEC sp_get_all_district_latest")
                //    .ToList();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictDB::DistrictGetAll_Latest() : Error executing sproc sp_get_all_district_latest"));
            }

            return districtList.ToArray();
        }

        public int UpdateDistrictByToken(JToken districtToken)
        {
            District district = new();
            Common common = new();
            int returnCode = 0;

            try
            {
                district.district_id = districtToken.Value<int?>("region_id") ?? 0;
                district.district_matic_key = districtToken.Value<string>("address");

                district.owner_name = districtToken.Value<string>("owner_nickname") ?? "Not Found";
                district.owner_avatar_id = districtToken.Value<int?>("owner_avatar_id") ?? 0;
                district.owner_matic = districtToken.Value<string>("address") ?? "Not Found";

                district.active_from = common.TimeFormatStandardFromUTC(districtToken.Value<string>("active_from") ?? "", null);
                district.plots_claimed = districtToken.Value<int?>("claimed_cnt") ?? 0;
                district.building_count = districtToken.Value<int?>("buildings_cnt") ?? 0;
                district.land_count = districtToken.Value<int?>("lands") ?? 0;

                district.energy_tax = districtToken.Value<int?>("energy_tax") ?? 0;
                district.production_tax = districtToken.Value<int?>("production_tax") ?? 0;
                district.commercial_tax = districtToken.Value<int?>("commercial_tax") ?? 0;
                district.citizen_tax = districtToken.Value<int?>("citizens_tax") ?? 0;              // Note that MCP uses citizens_tax - plural form.  In MetaverseMap using standard of singular naming system wide, only when using collections in code may use plural - but usually use List and keep singular name

                JToken constructionTax = districtToken.Value<JToken>("construction_taxes");
                if (constructionTax != null && constructionTax.HasValues)
                {
                    district.construction_energy_tax = constructionTax.Value<int?>(0) ?? 0;
                    district.construction_industry_production_tax = constructionTax.Value<int?>(1) ?? 0;
                    district.construction_residential_tax = constructionTax.Value<int?>(2) ?? 0;
                    district.construction_commercial_tax = constructionTax.Value<int?>(3) ?? 0;
                    district.construction_municipal_tax = constructionTax.Value<int?>(4) ?? 0;
                }
                else
                {
                    district.construction_energy_tax = 0;
                    district.construction_industry_production_tax = 0;
                    district.construction_residential_tax = 0;
                    district.construction_commercial_tax = 0;
                    district.construction_municipal_tax = 0;
                }

                district.distribution_period = districtToken.Value<int?>("distribution_period") ?? 0;
                district.insurance_commission = districtToken.Value<int?>("insurance_commission") ?? 0;

                district.resource_zone = districtToken.Value<int?>("resources") ?? 0;
                district.land_plot_price = districtToken.Value<int?>("land_plot_price") ?? 0;

                returnCode = DistrictUpdate(district);

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("UpdateDistrictByToken() : Error District_id: ", district.district_id.ToString()));
            }

            return returnCode;
        }

        public int DistrictUpdate(District district)
        {
            int result, instance = 0;
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
                result = _context.Database.ExecuteSqlRaw("EXEC @update_instance = dbo.sp_owner_summary_district_insert @district_id", new[] { districtParameter, updateInstance });

                //Use the new update_instance returned from summary sproc and assign to new district row.
                district.update_instance = instance = (int)updateInstance.Value;
                district.last_update = DateTime.Now;

                _context.district.Add(district);
                _context.SaveChanges();

                //Update summary totals for district
                updateInstance.Direction = System.Data.ParameterDirection.Input;    // reusing parameter but now using as input - not output.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_district_update_by_instance @district_id, @update_instance", new[] { districtParameter, updateInstance });


            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("DistrictDB::DistrictUpdate() : Error updating district_id = ", district.district_id));
            }

            return instance;
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
                logException(ex, String.Concat("DistrictDB::DistrictAddOrUpdate() : Error updating district_id = ", district.district_id));
            }

            return 0;
        }

        public int ArchiveOwnerSummaryDistrict()
        {
            int result = 0;
            try
            {
                //exec sproc - create a dup set of plots within Archive table if not previously archived.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_owner_summary_district_archive");

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("DistrictDB::ArchiveOwnerSummaryDistrict() : Error Archiving OwnerSummaryDistrict row using sproc sp_owner_summary_district_archive "));
                _context.LogEvent(log);
            }

            return result;
        }
    }
}
