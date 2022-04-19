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
    public class PlotManage : ServiceBase
    {
        public PlotManage(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public async Task<JObject> GetPlotMCP(int posX, int posY)
        {
            string content = string.Empty;
            JObject jsonContent = null;

            try
            {
                serviceUrl = "https://ws-tron.mcp3d.com/land/get";
                // POST REST WS
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"x\": \"" + posX.ToString() + "\",\"y\": \"" + posY.ToString() + "\"}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Concat(" X:", posX, " Y:", posY));

                if (content.Length != 0)
                {                    
                    jsonContent = JObject.Parse(content);
                }
            }
            catch (Exception ex)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetPlot() : Error Adding/update Plot X:", posX, " Y:", posY));
                    _context.LogEvent(ex.Message);
                }
            }

            return jsonContent;
        }
    }       
}
