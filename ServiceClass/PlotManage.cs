using MetaverseMax.Database;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text;

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
            JObject jsonContent;
            List<int> citizenList = new();
            JArray citizenArray;

            try
            {
                jsonContent = Task.Run(() => GetPlotMCP(posX, posY)).Result;

                // If plotId is passed, then posX and posY not needed. Indicates plot already exists based on table key
                if (jsonContent != null)
                {
                    plotMatched = plotDB.AddOrUpdatePlot(jsonContent, posX, posY, plotId, saveEvent);

                    // Find Citizen token_ids currently assigned to plot - used by features such as ranking.
                    citizenArray = jsonContent.Value<JArray>("citizens") ?? new();
                    plotMatched.citizen = citizenArray.Select(c => (c.Value<int?>("id") ?? 0)).ToList();
                }

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
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("PlotManage::GetPlotMCP() : Error Adding/update Plot X:", posX, " Y:", posY));
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
        public int RemoveMegaHugePlot(List<Plot> plotList)
        {
            int megaHugeplotsRemoved = plotList.Count;
            List<Plot> filteredPlotsMegaHuge = plotList.Where(x => x.building_level == 7 || x.building_level == 6).ToList();
            List<int> tokenIdList = filteredPlotsMegaHuge.GroupBy(x => x.token_id).Select(grp => grp.Key).ToList();

            //List<Plot> filteredPlots1to5 = plotList.Where(x => x.building_level < 6).ToList();
            plotList.RemoveAll(x => x.building_level == 7 || x.building_level == 6);


            for (int counter = 0; counter < tokenIdList.Count; counter++)
            {
                plotList.Add(filteredPlotsMegaHuge.Where(x => x.token_id == tokenIdList[counter]).First());
            }

            megaHugeplotsRemoved -= plotList.Count;

            return megaHugeplotsRemoved;
        }

        // Remove [District Unclaimed Plots] from passed plot list where district claimed plots total is unchanged since last nightly sync run
        public int RemoveDistrictPlots(List<DistrictName> districtListMCPBasic, List<Plot> plotList)
        {
            DistrictManage districtManage = new(_context, worldType);
            DistrictDB districtDB = new(_context);
            int unclaimedPlotRemovedCount = 0;

            List<District> districtOpened = districtDB.DistrictGetAll_Latest().ToList();

            foreach (District district in districtOpened)
            {
                if (districtListMCPBasic.Where(r => r.district_id == district.district_id && r.claimed_cnt == district.plots_claimed).Any())
                {
                    // As Plot List is reducing must start from end to avoid missing plot comparision checks (as count is reducing)
                    for (int i = plotList.Count - 1; i >= 0; i--)
                    {
                        // Remove plots from process list, if matching district and plot is unclaimed
                        if (plotList[i].district_id == district.district_id && plotList[i].unclaimed_plot == true)
                        {
                            plotList.Remove(plotList[i]);
                            unclaimedPlotRemovedCount++;
                        }
                    }
                }
            }

            return unclaimedPlotRemovedCount;
        }

        // Remove all empty owned plots && built plots with unchanged IP
        // NOTE: Difference between count (plots removed) VS count (plots updated) is due to (a) [plots updated] is buildings and Empty plot count with 1 or more plots per building and (b) plot removed is count of individual plots
        public int RemoveAccountPlot(int waitPeriodMS, List<OwnerChange> ownerChangeList, List<DistrictName> districtListMCPBasic, List<Plot> plotList)
        {
            int totalPlotsRemoved = 0;
            OwnerManage ownerManage = new(_context, worldType);
            AlertManage alert = new(_context, worldType);
            OwnerChange ownerChange = null;
            DistrictName districtName = null;
            List<Plot> buildingPlotList = null;
            bool ownerMonumentStateChanged = false, districtPOIStateChanged = false;
            PlotDB plotDB = new PlotDB(_context, worldType);
            CitizenManage citizen = new(_context, worldType);

            int buildingTypeId = 0, districtId = 0, tokenId = 0, storedInfluenceBonus = 0, influenceBonus = 0, storedInfluence = 0, influence = 0, staminaAlertCount =0 ;
            int buildingUpdatedCount = 0, emptyPlotsUpdatedCount = 0;
            int storedApp123bonus = 0, storedApp4 = 0, storedApp5 = 0, storedInfluenceInfo = 0;

            // Need to process min of 1 plot per account, only filter accounts with 2 or more plots.
            List<string> accountWithMin2Plot = plotList.Where(r => r.owner_matic is not null)
                .GroupBy(c => c.owner_matic)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key.ToLower())
                .ToList();


            // Get owner lands, check if MCP plot is empty then remove from plot sync list (if also empty in local db)
            for (int i = 0; i < accountWithMin2Plot.Count; i++)
            {
                staminaAlertCount = 0;
                ownerChange = ownerChangeList.Where(x => x.owner_matic_key == accountWithMin2Plot[i]).FirstOrDefault();
                ownerMonumentStateChanged = ownerChange == null ? false : ownerChange.monument_activated || ownerChange.monument_deactivated;  // if either flag enabled, return true - [state has changed].

                // NOTE owner>lands service returns 1x plot per building - so will skip some plots used in Huge and Mega - using token_id to match buildings.                
                JArray lands = Task.Run(() => ownerManage.GetOwnerLandsMCP(accountWithMin2Plot[i])).Result;
                WaitPeriodAction(waitPeriodMS).Wait();

                // Update local db with partial plot data updates. [Use Full plot update plotManage.AddOrUpdatePlot() if IP has changed]
                // CHECK land token exist - it may have been recently created and not in local db
                if (lands == null)
                {
                    continue;       // All lands sold/transfered since last sync - run full sync process on these plots - dont filter.
                }
                else
                {                    
                    // CHECK if ALL owner lands have been sold/transfered since last sync, or only 1 land remains, then run full process sync on that plot/building. Needed for account avatar and name check
                    if (lands.Count == 0)
                    {
                        continue;
                    }
                    else if (lands.Count == 1)
                    {
                        // Check if that single land has low-stamina alert.
                        if (citizen.CheckCitizenStamina(lands[0].Value<JArray>("citizens"), lands[0].Value<int?>("building_type_id") ?? 0))
                        {
                            alert.AddLowStaminaAlert(accountWithMin2Plot[i], 1);
                        }
                    }

                    // Skip first account plot (start at 1 not 0) - full sync first account plot will retrive latest avatar and name used by account.
                    for (int landIndex = 1; landIndex < lands.Count; landIndex++)
                    {

                        buildingTypeId = lands[landIndex].Value<int?>("building_type_id") ?? 0;
                        tokenId = lands[landIndex].Value<int?>("token_id") ?? 0;

                        buildingPlotList = _context.plot.Where(x => x.token_id == tokenId).ToList();
                        if (buildingPlotList.Count == 0 || buildingPlotList[0].owner_matic.ToLower() != accountWithMin2Plot[i])
                        {
                            continue;   // if newly minted plot with new token OR plot sold/transfer, then get/process full plot - not partial.
                        }

                        influenceBonus = lands[landIndex].Value<int?>("influence_bonus") ?? 0;
                        influence = lands[landIndex].Value<int?>("influence") ?? 0;
                        storedInfluenceBonus = buildingPlotList[0].influence_bonus ?? 0;
                        storedInfluence = buildingPlotList[0].influence ?? 0;
                        storedInfluenceInfo = buildingPlotList[0].influence_info ?? 0;
                        storedApp123bonus = buildingPlotList[0].app_123_bonus ?? 0;
                        storedApp4 = buildingPlotList[0].app_4_bonus ?? 0;
                        storedApp5 = buildingPlotList[0].app_5_bonus ?? 0;

                        districtId = lands[landIndex].Value<int?>("region_id") ?? 0;
                        districtName = districtListMCPBasic.Where(x => x.district_id == districtId).FirstOrDefault();
                        districtPOIStateChanged = districtName == null ? false : districtName.poi_activated || districtName.poi_deactivated;

                        // NOTE: Empty plot needs to be updated on each nighly sync - as empty plot can be set For_sale or sale price changed.
                        // NOTE_2: Newly build POI and monuments wont trigger a state change until next sync, unless account manually viewed and plots updated before sync
                        // NOTE_3: The influence attributes check is needed as building may be destroyed reverting back to empty plot, plot influance fields will need full sync to all reflect current 0 value.
                        if ((buildingTypeId == (int)BUILDING_TYPE.EMPTY_LAND || buildingTypeId == (int)BUILDING_TYPE.POI)
                            && influence == 0 && influenceBonus == 0 && storedInfluenceInfo == 0)
                        {
                            plotDB.UpdatePlotPartial(lands[landIndex], false);
                            emptyPlotsUpdatedCount++;

                            totalPlotsRemoved += plotList.RemoveAll(x =>
                               x.owner_matic == accountWithMin2Plot[i] &&
                               x.token_id == tokenId);
                        }
                        // CHECK: if no Plot IP change (due to POI or Monument state change)
                        //  AND no IP bonus change - or anomoly found with stored_app_bonus components vs influenceBonus,
                        //  AND no change in IP due to nearby building (as influence_info would need to be recalculated)
                        //  THEN partial update can proceed. (no full update required)
                        else if (ownerMonumentStateChanged == false && districtPOIStateChanged == false
                            && storedInfluenceBonus == influenceBonus
                            && influence == storedInfluence
                            && influenceBonus == (storedApp123bonus + storedApp4 + storedApp5))
                        {
                            plotDB.UpdatePlotPartial(lands[landIndex], false);
                            buildingUpdatedCount++;

                            // Newly purchased will be handled correctly in UpdatePlotPartial triggering a full plot process. Any sold plots wont get filtered.
                            totalPlotsRemoved += plotList.RemoveAll(x =>
                               x.owner_matic == accountWithMin2Plot[i] &&
                               x.token_id == tokenId);
                        }
                        staminaAlertCount = citizen.CheckCitizenStamina(lands[landIndex].Value<JArray>("citizens"), lands[landIndex].Value<int?>("building_type_id") ?? 0) ? ++staminaAlertCount : staminaAlertCount;
                    }

                    // Add Owner Alert if any lands have citizens with low stamina
                    if (staminaAlertCount > 0)
                    {
                        alert.AddLowStaminaAlert(accountWithMin2Plot[i], staminaAlertCount);
                    }
                }

            }

            _context.LogEvent(String.Concat("PlotManage:RemoveEmptyPlot() :  Empty Plots updated : ", emptyPlotsUpdatedCount, ",  Buildings updated : ", buildingUpdatedCount));

            return totalPlotsRemoved;
        }

        // Lazy update of plots using Full plot update 
        // Used to improve accuracy of ranking feature, where user load portfolio - identifing plot.influence change >> needs full update for Ranking
        public async Task<int> FullUpdateBuildingAsync(List<PlotCord> tokenIdList)
        {
            // Generate a new dbContext as a safety measure - insuring log is recorded.  Service trigged has already ended.
            using (var _contexJob = new MetaverseMaxDbContext(worldType))
            {
                PlotManage plotManage = new PlotManage(_contexJob, worldType);

                foreach (PlotCord plotCord in tokenIdList)
                {
                    plotManage.AddOrUpdatePlot(plotCord.plotId, plotCord.posX, plotCord.posY, false);
                    await Task.Delay(1000);
                }

                _contexJob.SaveChanges();
            }
            return 0;
        }

        public string[] UnitTest_GetSyncPlotList()
        {
            LAND_TYPE landType = worldType switch { WORLD_TYPE.TRON => LAND_TYPE.TRON_BUILDABLE_LAND, WORLD_TYPE.BNB => LAND_TYPE.BNB_BUILDABLE_LAND, WORLD_TYPE.ETH => LAND_TYPE.TRON_BUILDABLE_LAND, _ => LAND_TYPE.TRON_BUILDABLE_LAND };
            int jobInterval = 50;
            DistrictManage districtManage = new(_context, worldType);
            int plotCount, emptyPlotFilterCount = 0, unclaimedPlotRemovedCount = 0;

            List<OwnerChange> ownerChangeList = new();
            List<DistrictName> districtListMCPBasic = new();
            List<Plot> plotList = _context.plot.Where(r => r.land_type == (int)landType).ToList();
            plotCount = plotList.Count;

            emptyPlotFilterCount = RemoveAccountPlot(jobInterval, ownerChangeList, districtListMCPBasic, plotList);     // removes all account plots from process list matching criteria.

            List<DistrictName> districtBasic = Task.Run(() => districtManage.GetDistrictBasicFromMCP(true)).Result;

            unclaimedPlotRemovedCount = RemoveDistrictPlots(districtBasic, plotList);                                   // remove all unclaimed plots from districts that have not changed since last sync

            _context.SaveChanges();

            return new string[] {
                string.Concat("Total Plots: ", plotCount),
                string.Concat("Owner Empty Plots filtered: ", emptyPlotFilterCount),
                string.Concat("District unclaimed Plots filtered: ", unclaimedPlotRemovedCount),
                string.Concat("Plot count(to process): ", plotList.Count)
                };
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
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotManage::GetPlotsMCP() : Error occured"));
            }

            return pollingPlot;
        }
    }
}
