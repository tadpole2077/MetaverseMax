using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class DistrictPerkManage
    {
        private readonly MetaverseMaxDbContext _context;

        public DistrictPerkManage(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;
        }

        public IEnumerable<DistrictPerk> GetPerks(int districtId, int updateInstance)
        {
            DistrictPerkDB districtPerkDB = new(_context);

            return districtPerkDB.PerkGetAll(districtId, updateInstance);
        }

        public IEnumerable<DistrictPerk> GetPerks()
        {   
            List<DistrictPerk> districtPerkList = new();
            string content = string.Empty;
            Common common = new();

            try
            {
                // POST from Land/Get REST WS
                //byte[] byteArray = Encoding.ASCII.GetBytes("{\"region_id\": " + district_id.ToString() + "}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/perks/districts");
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream dataStream = request.GetRequestStream();
                //dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Ensure correct dispose of WebRespose IDisposable class even if exception
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader reader = new(response.GetResponseStream());
                    content = reader.ReadToEnd();
                }

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
