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
            int retryCount = 0;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            while (returnCode == RETURN_CODE.ERROR && retryCount < 3)
            {
                try
                {
                    retryCount++;

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

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    if (_context != null)
                    {
                        _context.LogEvent(String.Concat("PlotManage::GetPlot() : Error Adding/update Plot X:", posX, " Y:", posY));
                        _context.LogEvent(ex.Message);
                    }
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetPlot() : retry successful - no ", retryCount));
                }
            }

            return jsonContent;
        }

        public List<Plot> CheckEmptyPlot(int waitPeriodMS)
        {
            OwnerManage ownerManage = new(_context);

            // From local DB, get list of owners with more then 1 Empty Plot.
            List<Plot> plotList = _context.plot.Where(r => r.land_type == 1).ToList();
            List<string> emptyPlotAccount = plotList.Where(r => r.building_id == 0 && r.owner_matic is not null)
                .GroupBy(c => c.owner_matic)
                .Where(grp => grp.Count() > 2)
                .Select(grp => grp.Key).ToList();


            // Get owner lands, check if MCP plot is empty then remove from plot sync list (if also empty in local db)
            for (int i = 0; i < emptyPlotAccount.Count; i++)
            {
                Task.Run(() => ownerManage.GetOwnerLands(emptyPlotAccount[i])).Wait();

                Task.Run(async () => { await WaitPeriodAction(waitPeriodMS); }).Wait();

                foreach (OwnerLand ownerLand in ownerManage.ownerData.owner_land)
                {
                    if (ownerLand.building_type == 0)
                    {
                        // Remove plots from process list, if owner is same as local and plot is empty - roughly 1/3 of owned plots on Tron 05/2022
                        Plot targetPlot = plotList.Where(x => x.token_id == ownerLand.token_id && x.owner_matic == emptyPlotAccount[i] && x.building_id == 0).FirstOrDefault();
                        if (targetPlot != null)
                        {
                            plotList.Remove(targetPlot);
                        }
                    }
                }
            }

            return plotList;
        }
    }       
}
