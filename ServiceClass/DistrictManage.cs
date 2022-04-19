using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class DistrictManage : ServiceBase
    {
        public DistrictManage(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public async Task<DistrictWeb> GetDistrictMCP(int district_id)
        {
            DistrictWeb district = new();
            CitizenManage citizen = new(_context);
            string content = string.Empty;
            Common common = new();

            try
            {
                // POST REST WS
                serviceUrl = "https://ws-tron.mcp3d.com/regions/list";
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"region_id\": " + district_id.ToString() + "}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, district_id.ToString());

                if (content.Length == 0)
                {
                    district.owner_name = "Unclaimed District";
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districtData = jsonContent.Value<JArray>("stat");
                    if (districtData != null && districtData.HasValues)
                    {
                        JToken districtToken = districtData[0];
                        district.district_id = districtToken.Value<int?>("region_id") ?? 0;
                        district.owner_name = districtToken.Value<string>("owner_nickname") ?? "Not Found";
                        district.owner_url = citizen.AssignDefaultOwnerImg(districtToken.Value<string>("owner_avatar_id") ?? "");
                        district.owner_matic = districtToken.Value<string>("address") ?? "Not Found";

                        district.active_from = common.TimeFormatStandard(districtToken.Value<string>("active_from") ?? "", null);
                        district.plots_claimed = districtToken.Value<int?>("claimed_cnt") ?? 0;
                        district.building_count = districtToken.Value<int?>("buildings_cnt") ?? 0;
                        district.land_count = districtToken.Value<int?>("lands") ?? 0;
                        district.energy_tax = districtToken.Value<int?>("energy_tax") ?? 0;
                        district.production_tax = districtToken.Value<int?>("production_tax") ?? 0;
                        district.commercial_tax = districtToken.Value<int?>("commercial_tax") ?? 0;
                        district.citizen_tax = districtToken.Value<int?>("citizens_tax") ?? 0;
                    }

                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrict() : Error District_id: ", district_id.ToString()));
                    _context.LogEvent(log);
                }
            }

            return district;
        }

        // Update All active districts from MCP REST WS, update owner summary per district using local db plot data
        public async Task<int> UpdateAllDistricts(string secureToken)
        {
            DistrictDB districtDB;
            JToken districtToken;

            string content = string.Empty;
            int districtUpdateCount = 0;
            try
            {
                // As this service could be abused as a DDOS a security token is needed.
                if (!secureToken.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return districtUpdateCount;
                    ;
                }

                districtDB = new DistrictDB(_context);

                // POST REST WS
                serviceUrl = "https://ws-tron.mcp3d.com/regions/list";
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Empty);

                if (content.Length > 0)
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districts = jsonContent.Value<JArray>("stat");
                    if (districts != null && districts.HasValues)
                    {
                        for (int index = 0; index < districts.Count; index++)
                        {
                            districtToken = districts[index];
                            districtDB.UpdateDistrictByToken(districtToken);

                            districtUpdateCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("UpdateAllOpenedDistricts() : Error "));
                    _context.LogEvent(log);
                }
            }


            return districtUpdateCount;
        }
    }
}
