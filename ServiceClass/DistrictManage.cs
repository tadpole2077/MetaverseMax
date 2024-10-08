﻿using Newtonsoft.Json.Linq;
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
            bool getTaxHistory = true;
            bool perksDetail = true;

            try
            {
                district = districtDB.DistrictGet(districtId);
                
                // Defensive code - check no district found in local db (may be unclaimed)
                if (district.district_id == 0)
                {

                    districtWeb.owner_name = "Unclaimed District";
                }
                else
                {
                    districtHistory_1Mth = districtDB.DistrictGet_History1Mth(districtId, district.last_update);

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
            DistrictWeb districtWeb = new();
            CitizenManage citizen = new(_context, worldType);
            string content = string.Empty;
            ServiceCommon common = new();

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
                    districtWeb.owner_name = "Unclaimed District";
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districtData = jsonContent.Value<JArray>("stat");
                    if (districtData != null && districtData.HasValues)
                    {
                        JToken districtToken = districtData[0];
                        districtWeb.district_id = districtToken.Value<int?>("region_id") ?? 0;
                        districtWeb.owner_name = districtToken.Value<string>("owner_nickname") ?? "Not Found";
                        districtWeb.owner_url = citizen.AssignDefaultOwnerImg(districtToken.Value<string>("owner_avatar_id") ?? "");
                        districtWeb.owner_matic = districtToken.Value<string>("address") ?? "Not Found";

                        districtWeb.active_from = common.LocalTimeFormatStandardFromUTC(districtToken.Value<string>("active_from") ?? "", null);
                        districtWeb.plots_claimed = districtToken.Value<int?>("claimed_cnt") ?? 0;
                        districtWeb.building_count = districtToken.Value<int?>("buildings_cnt") ?? 0;
                        districtWeb.land_count = districtToken.Value<int?>("lands") ?? 0;
                        districtWeb.energy_tax = districtToken.Value<int?>("energy_tax") ?? 0;
                        districtWeb.production_tax = districtToken.Value<int?>("production_tax") ?? 0;
                        districtWeb.commercial_tax = districtToken.Value<int?>("commercial_tax") ?? 0;
                        districtWeb.citizen_tax = districtToken.Value<int?>("citizens_tax") ?? 0;
                    }

                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::GetDistrictMCP() : Error District_id: ", district_id.ToString()));
            }

            return districtWeb;
        }

        // Update All active districts from MCP REST WS
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

                            District district = districtDB.MapDistrictByToken(districtToken);
                            districtDB.DistrictUpdate(district);

                            UpdateDistrictOwner(district.district_avatar_id, district.district_owner_name, district.owner_matic);

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

        public bool UpdateDistrictOwner(int districtAvatarId, string districtOwnerName, string ownerMatic)
        {
            MetaverseMaxDbContext_UNI _contextUni = new();
            OwnerNameDB ownerNameDB = new(_context);
            OwnerUniDB ownerUniDB = new(_contextUni);

            if (!string.IsNullOrEmpty(ownerMatic))
            {

                // Corner Case - Owner of a District but owns no plots - check owner exists if not create account
                OwnerChange ownerChange = new()
                {
                    owner_avatar_id = districtAvatarId,
                    owner_name = districtOwnerName,
                    owner_matic_key = ownerMatic,
                };

                ownerNameDB.UpdateOwnerName(ownerChange, false);
                ownerUniDB.CheckLink(ownerChange, worldType);
            }

            _contextUni.Dispose();

            return true;
        }

        public async Task<int> UpdateDistrict(int district_id)
        {
            District district;
            string content = string.Empty;
            int updateInstance = 0;

            try
            {
                // POST REST WS                
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.REGIONS_LIST, WORLD_TYPE.BNB => BNB_WS.REGIONS_LIST, WORLD_TYPE.ETH => ETH_WS.REGIONS_LIST, _ => TRON_WS.REGIONS_LIST };
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new("{\"region_id\": " + district_id.ToString() + "}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, district_id.ToString());

                // Defensive code - Check District exists
                if (content.Length != 0)
                {                
                    JObject jsonContent = JObject.Parse(content);
                    JArray districtData = jsonContent.Value<JArray>("stat");
                    if (districtData != null && districtData.HasValues)
                    {
                        district = districtDB.MapDistrictByToken(districtData[0]);
                        UpdateDistrictOwner(district.district_avatar_id, district.district_owner_name, district.owner_matic);
                        updateInstance = districtDB.DistrictUpdate(district);                        
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("DistrictManage::UpdateDistrict() : Error District_id: ", district_id.ToString()));
            }

            return updateInstance;
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
