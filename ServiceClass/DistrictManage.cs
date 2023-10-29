using Newtonsoft.Json.Linq;
using System.Text;
using MetaverseMax.BaseClass;
using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class DistrictManage : ServiceBase
    {
        private DistrictDB districtDB;

        public DistrictManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;

            districtDB = new(_context);
        }


        public DistrictWeb GetDistrict(int districtId)
        {
            DistrictWeb districtWeb = new();
            DistrictWebMap districtWebMap = new(_context, worldType);
            PlotDB plotDB = new(_context, worldType);

            District district, districtHistory_1Mth = new();
            DistrictContent districtContent = new();
            bool getTaxHistory = true;

            string content = string.Empty;
            Common common = new();
            bool perksDetail = true;

            try
            {
                district = districtDB.DistrictGet(districtId);
                districtHistory_1Mth = districtDB.DistrictGet_History1Mth(districtId, district.last_update);

                if (district.district_id == 0)
                {

                    district.owner_name = "Unclaimed District";
                }
                else
                {
                    districtWeb = districtWebMap.MapData_DistrictWeb(district, districtHistory_1Mth, perksDetail, getTaxHistory);                    
                    districtWeb.custom_count = plotDB.GetCustomCountByDistrict(districtId);
                    districtWeb.parcel_count = plotDB.GetParcelCountByDistrict(districtId) - districtWeb.custom_count;
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("GetDistrict() : Error District_id: ", districtId.ToString()));
            }

            return districtWeb;
        }

        // Gets List of Purchased Districts from REST WS CAll to MCP (All returned districts may not yet be opened)
        public async Task<List<DistrictName>> GetDistrictBasicFromMCP(bool isOpened)
        {
            List<DistrictName> districtList = null;
            string content = string.Empty;
            try
            {
                // POST REST WS
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.REGIONS_LIST, WORLD_TYPE.BNB => BNB_WS.REGIONS_LIST, WORLD_TYPE.ETH => ETH_WS.REGIONS_LIST, _ => TRON_WS.REGIONS_LIST };

                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(serviceUrl, stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Empty);

                districtList = new();
                if (content.Length == 0)
                {
                    districtList.Add(new DistrictName { district_id = 0, district_name = "Loading Issue" });
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districts = jsonContent.Value<JArray>("stat");
                    if (districts != null && districts.HasValues)
                    {
                        for (int index = 0; index < districts.Count; index++)
                        {
                            JToken districtToken = districts[index];
                            districtList.Add(new DistrictName()
                            {
                                district_id = districtToken.Value<int?>("region_id") ?? 0,
                                district_name = "",
                                building_cnt = districtToken.Value<int?>("building_cnt") ?? 0,
                                claimed_cnt = districtToken.Value<int?>("claimed_cnt") ?? 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::GetDistrictsFromMCP() : Error "));
            }

            return districtList;
        }

        public async Task<DistrictWeb> GetDistrictMCP(int district_id)
        {
            DistrictWeb district = new();
            CitizenManage citizen = new(_context, worldType);
            string content = string.Empty;
            Common common = new();

            try
            {
                // POST REST WS
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.REGIONS_LIST, WORLD_TYPE.BNB => BNB_WS.REGIONS_LIST, WORLD_TYPE.ETH => ETH_WS.REGIONS_LIST, _ => TRON_WS.REGIONS_LIST };
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

                        district.active_from = common.LocalTimeFormatStandardFromUTC(districtToken.Value<string>("active_from") ?? "", null);
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
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::GetDistrictMCP() : Error District_id: ", district_id.ToString()));
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
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.REGIONS_LIST, WORLD_TYPE.BNB => BNB_WS.REGIONS_LIST, WORLD_TYPE.ETH => ETH_WS.REGIONS_LIST, _ => TRON_WS.REGIONS_LIST };
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
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::UpdateAllDistricts() : Error "));
            }


            return districtUpdateCount;
        }

        public async Task<int> UpdateDistrict(int district_id)
        {
            District district = new();
            string content = string.Empty;
            Common common = new();
            int returnCode = 0;

            try
            {
                // POST REST WS                
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.REGIONS_LIST, WORLD_TYPE.BNB => BNB_WS.REGIONS_LIST, WORLD_TYPE.ETH => ETH_WS.REGIONS_LIST, _ => TRON_WS.REGIONS_LIST };
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
                        returnCode = districtDB.UpdateDistrictByToken(districtData[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::UpdateDistrict() : Error District_id: ", district_id.ToString()));
            }

            return returnCode;
        }

        public int ArchiveOwnerSummaryDistrict()
        {
            int returnCode = 0;
            try
            {
                districtDB.ArchiveOwnerSummaryDistrict();
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::ArchiveOwnerSummaryDistrict(): Error occured during arhive call"));
            }

            return returnCode;
        }
    }
}
