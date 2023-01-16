using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class DistrictPerkManage : ServiceBase
    {
        public DistrictPerkManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        public IEnumerable<DistrictPerk> GetPerks(int districtId, int updateInstance)
        {
            DistrictPerkDB districtPerkDB = new(_context);

            return districtPerkDB.PerkGetAll(districtId, updateInstance);
        }

        public async Task<IEnumerable<DistrictPerk>> GetPerks()
        {   
            List<DistrictPerk> districtPerkList = new();
            string content = string.Empty;
            Common common = new();

            try
            {
                // POST REST WS
                serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.PERKS_DISTRICT, WORLD_TYPE.BNB => BNB_WS.PERKS_DISTRICT, WORLD_TYPE.ETH => ETH_WS.PERKS_DISTRICT, _ => TRON_WS.PERKS_DISTRICT };
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

                    // Each district has a 0.n collection of Perks
                    foreach(KeyValuePair<string,JToken> dPerks in jsonContent)
                    {
                        JToken dPerk = dPerks.Value;
                        JArray perks = dPerk.Value<JArray>("perks");

                        foreach(JToken perk in perks)
                        {
                            districtPerkList.Add(new DistrictPerk()
                            {
                                district_id = perk.Value<int>("districtId"),
                                perk_id = perk.Value<int>("perkId"),
                                perk_level = perk.Value<int>("level"),
                            });
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("DistrictPerkManage::GetPerks() : Error Getting all District Perks "));
                    _context.LogEvent(log);
                }
            }

            return districtPerkList.ToArray();
        }
    }
}
