using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class PlotManage : ServiceBase
    {        
        public PlotManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }


        public static async void PollWorldPlots(object parameters)
        {
            MetaverseMaxDbContext _context = null;
            PlotManage plotManage = null;
            int x = 0, y = 0;
            WORLD_TYPE worldType = WORLD_TYPE.TRON; // just default

            try
            {
                ArrayList threadParameters = (ArrayList)parameters;
                int startPosX = (int)threadParameters[0];
                int startPosY = (int)threadParameters[1];
                int endPosX = (int)threadParameters[2];
                int endPosY = (int)threadParameters[3];
                int jobInterval = (int)threadParameters[4];
                worldType = (WORLD_TYPE)threadParameters[5];

                _context = new MetaverseMaxDbContext(worldType);
                plotManage = new PlotManage(_context, worldType);

                // 250,000 plot locations - 1 second per plot - 69 hours. 100ms wait per plot = 7hrs.
                // Iterate though each of the plots in the target zone, add or update db row to match
                for (x = Convert.ToInt32(startPosX); x <= Convert.ToInt32(endPosX); x++)
                {
                    for (y = Convert.ToInt32(startPosY); y <= Convert.ToInt32(endPosY); y++)
                    {
                        await Task.Delay(jobInterval);      // Typically minimum interval using this Delay thread method is about 50 miliseconds

                        plotManage.AddOrUpdatePlot(0, x, y, false);
                    }
                    _context.SaveChanges();
                    _context.Dispose();
                    _context = new MetaverseMaxDbContext(worldType);
                    plotManage = new PlotManage(_context, worldType);
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("PlotManage:AddOrUpdatePlot() : Error Adding/update Plot X:", x, " Y:", y));
            }

            return;
        }


        public Plot AddOrUpdatePlot(int plotId, int posX, int posY, bool saveEvent)
        {                        
            Plot plotMatched = null;
            PlotDB plotDB = new(_context);
            CitizenManage citizen = new(_context, worldType);
            Building building = new();
            JObject jsonContent;

            try
            {
                jsonContent = Task.Run(() => GetPlotMCP(posX, posY)).Result;

                // If plotId is passed, then posX and posY not needed. Indicates plot already exists based on table key
                plotMatched = plotDB.AddOrUpdatePlot(jsonContent, posX, posY, plotId, saveEvent);

                // Special Cases : update related building plots
                //  Huge updated to MEGA - the other huge will also get processed and its paired plot should also get updated.
                //  MEGA or HUGE distroyed -  all related building plots will be reset to empty, on next nightly sync will be picked up as new buildings (if built)
                if (plotMatched.building_level == 6 || plotMatched.building_level == 7)
                {
                    // Need to first commit any local Entity framework stored records, needed for sproc to apply data to related db plot records.
                    _context.SaveChanges();
                    plotDB.UpdateRelatedBuildingPlot(plotMatched.plot_id);
                }
            }
            catch (Exception ex)
            {
                if (_context != null)
                {
                    DBLogger dbLogger = new(_context, worldType);
                    dbLogger.logException(ex, String.Concat("PlotManage:AddOrUpdatePlot() : Error Adding/update Plot X:", posX, " Y:", posY));
                }
            }

            return plotMatched;
        }
        public async Task<JObject> GetPlotMCP(int posX, int posY)
        {
            string content = string.Empty;
            JObject jsonContent = null;
            int retryCount = 0;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            while (returnCode == RETURN_CODE.ERROR && retryCount < 5)
            {
                try
                {
                    retryCount++;
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.LAND_GET, WORLD_TYPE.BNB => BNB_WS.LAND_GET, WORLD_TYPE.ETH => ETH_WS.LAND_GET, _ => TRON_WS.LAND_GET };
                    
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
                        _context.LogEvent(String.Concat("PlotManage::GetPlotMCP() : Error Adding/update Plot X:", posX, " Y:", posY));
                        _context.LogEvent(ex.Message);
                    }
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetPlotMCP() : retry successful - no ", retryCount));
                }
            }

            return jsonContent;
        }

        // Get only one plot for Mega or Huge building - only need to process 1 on nightly sync (NOTE might be an issue if MEGA/HUGE building is destroyed)
        public List<Plot> RemoveMegaHugePlot(List<Plot> plotList)
        {
            List<Plot> filteredPlotsMegaHuge = plotList.Where(x => x.building_level == 7 || x.building_level == 6).ToList();

            List<Plot> filteredPlots1to5 = plotList.Where(x => x.building_level < 6).ToList();

            List<int> tokenIdList = filteredPlotsMegaHuge.GroupBy(x => x.token_id).Select(grp => grp.Key).ToList();            
            for(int counter=0; counter < tokenIdList.Count; counter++){
                filteredPlots1to5.Add(filteredPlotsMegaHuge.Where(x => x.token_id == tokenIdList[counter]).First() );
            }

            return filteredPlots1to5;
        }

        // Remove Plots from passed list where district claimed plots has not changed since last nightly sync run
        public List<Plot> RemoveDistrictPlots(List<DistrictName> districtListMCPBasic, List<Plot> plotList)
        {
            DistrictManage districtManage = new(_context, worldType);
            DistrictDB districtDB = new(_context);

            List<District> districtOpened =  districtDB.DistrictGetAll_Latest().ToList();

            foreach (District district in districtOpened)
            {
                if (districtListMCPBasic.Where(r => r.district_id == district.district_id && r.claimed_cnt == district.plots_claimed).Any())
                {
                    // As Plot List is reducing must start from end to avoid missing plot comparision checks (as count is reducing)
                    for (int i = plotList.Count-1; i >= 0; i--)
                    {
                        // Remove plots from process list, if matching district and plot is unclaimed
                        if (plotList[i].district_id == district.district_id && plotList[i].unclaimed_plot == true)
                        {
                            plotList.Remove(plotList[i]);
                        }
                    }
                }
            }
            
            return plotList;
        }

        public List<Plot> CheckEmptyPlot(int waitPeriodMS, int districtId)
        {
            OwnerManage ownerManage = new(_context, worldType);
            LAND_TYPE landType = worldType switch { WORLD_TYPE.TRON => LAND_TYPE.TRON_BUILDABLE_LAND, WORLD_TYPE.BNB => LAND_TYPE.BNB_BUILDABLE_LAND, WORLD_TYPE.ETH => LAND_TYPE.TRON_BUILDABLE_LAND, _ => LAND_TYPE.TRON_BUILDABLE_LAND };

            // From local DB, get list of owners with more then 1 Empty Plot.
            List<Plot> plotList;
            List<string> emptyPlotAccount;
            
            // NOTE - testing by district only will show less matches for accounts with 2 or more empty plots VS eval with no district filter - due to count of 2 or more empty plot across all districts.
            if (districtId > 0) {
                plotList = _context.plot.Where(r => r.land_type == (int)landType && r.district_id == districtId).ToList();

                emptyPlotAccount = plotList.Where(r => r.building_id == 0 && r.owner_matic is not null && r.district_id == districtId )
                    .GroupBy(c => c.owner_matic)
                    .Where(grp => grp.Count() > 2)
                    .Select(grp => grp.Key).ToList();
            }
            else
            {
                plotList = _context.plot.Where(r => r.land_type == (int)landType).ToList();

                emptyPlotAccount = plotList.Where(r => r.building_id == 0 && r.owner_matic is not null)
                    .GroupBy(c => c.owner_matic)
                    .Where(grp => grp.Count() > 2)
                    .Select(grp => grp.Key).ToList();
            }

            // Get owner lands, check if MCP plot is empty then remove from plot sync list (if also empty in local db)
            for (int i = 0; i < emptyPlotAccount.Count; i++)
            {
                // NOTE owner>lands service returns 1x plot per building - so will be skip plots for Huge and Mega - use token_id to match buildings.
                //      This Call will also update the local db with partial plot data updates - its not the full sync. if plot was recently minted/transfer/sold then owner details will be updated.
                ownerManage.GetOwnerLands(emptyPlotAccount[i], false).Wait();

                WaitPeriodAction(waitPeriodMS).Wait();

                // CHECK if all owner lands have been sold/transfered since last sync then leave those plots for full sync process
                if (ownerManage.ownerData.owner_land != null)
                {
                    foreach (OwnerLand ownerLand in ownerManage.ownerData.owner_land)
                    {
                        // Check if empty - no build on MCP
                        if (ownerLand.building_type == 0)
                        {
                            // Defensive code : removal of 1 or more plots matching token_id - useful for huge/mega/poi/etc
                            // Only remove the Empty plot if local db shows its owned by this account - meaning plot may have been minted/transfer/sold since last sync - needs to be processed
                            plotList.RemoveAll(x => x.token_id == ownerLand.token_id && x.owner_matic == emptyPlotAccount[i]);
                        }
                    }
                }
            }

            return plotList;
        }

        public async Task<string> UnitTest_ActivePlotByDistrictDataSync(int districtId)
        {
            int jobInterval = 50;
            DistrictManage districtManage = new(_context, worldType);
            int emptyPlotFilterCount = 0;
            int districtPlotCount = 0;
            List<Plot> plotList;

            districtPlotCount =_context.plot.Where(r => r.land_type == 1 && r.district_id == districtId).ToList().Count;

            plotList = CheckEmptyPlot(jobInterval, districtId);              // removes all empty plots from process list where plot owner unchanged and still plot empty
            emptyPlotFilterCount = plotList.Count;

            List<DistrictName> districtBasic = await districtManage.GetDistrictBasicFromMCP(true);

            plotList = RemoveDistrictPlots(districtBasic, plotList);    // remove all unclaimed plots from districts that have not changed since last sync

            _context.SaveChanges();

            return string.Concat("District plot count: ", districtPlotCount,
                "  Plot count after removal of district owners with Empty Plots:  ", emptyPlotFilterCount,
                "  Plot count after removal of all District Empty Plots if district claimed plot count unchanged:", plotList.Count);
        }

        public WorldNameCollection GetWorldNames()
        {
            WorldNameCollection worldNameCollection = new();
            List<WorldName> worldNames = new();
            
            worldNames.Add(new WorldName() { id = 1, name = "Tron" });
            worldNames.Add(new WorldName() { id = 2, name = "BNB" });
            worldNames.Add(new WorldName() { id = 3, name = "ETH" });

            worldNameCollection.world_name = worldNames;

            return worldNameCollection;
        }

        public PollingPlot GetPlotsMCP(QueryParametersGetPlotMatric parameters)
        {
            PollingPlot pollingPlot = new();
            PlotDB plotDB;

            try
            {
                plotDB = new(_context);

                // As this service could be abused as a DDOS a security token is needed.
                if (parameters.secure_token == null || !parameters.secure_token.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return pollingPlot;
                }

                int jobInterval = Convert.ToInt32(string.IsNullOrEmpty(parameters.interval) ? 150 : parameters.interval);


                ArrayList threadParameters = new();
                threadParameters.Add(Convert.ToInt32(parameters.start_pos_x));
                threadParameters.Add(Convert.ToInt32(parameters.start_pos_y));
                threadParameters.Add(Convert.ToInt32(parameters.end_pos_x));
                threadParameters.Add(Convert.ToInt32(parameters.end_pos_y));
                threadParameters.Add(jobInterval);
                threadParameters.Add(worldType);


                // Assign delegate function type to parameter thread object           
                ParameterizedThreadStart pollWorldPlots = new(PollWorldPlots);

                // Create a new thread utilizing the parameter thread object
                Thread tPollWorldPlots = new(pollWorldPlots)
                {
                    // Next we set the priority of our thread to normal default setting
                    Priority = ThreadPriority.Normal
                };

                // Start execution of thread
                tPollWorldPlots.Start(threadParameters);

                // Select type query using LINQ returning a collection of row matching condition - selecting first row.               
                pollingPlot.last_plot_updated = plotDB.GetLastPlotUpdated();
                pollingPlot.status = "Polling World Started";
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return pollingPlot;
        }
    }       
}
