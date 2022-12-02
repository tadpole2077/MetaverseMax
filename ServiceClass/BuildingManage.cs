using MetaverseMax.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace MetaverseMax.ServiceClass
{
    public class BuildingManage : ServiceBase
    {
        private CitizenManage citizenManage;
        private Common common = new();
        private List<OwnerCitizenExt> ownerCitizenExt = new();
        private BuildingCollection buildingCollection = new();
        private const int USE_STORED_EFFICIENCY = -1;
        private BuildingHistory buildingHistory;

        public BuildingManage(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            citizenManage = new(_context);
        }

        public async Task<RETURN_CODE> UpdateIPRanking(int waitPeriodMS = 100)
        {
            for (int level = 1; level < 8; level++)
            {
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.RESIDENTIAL, level, true, waitPeriodMS);
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.INDUSTRIAL, level, true, waitPeriodMS);
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.PRODUCTION, level, true, waitPeriodMS);
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.ENERGY, level, true, waitPeriodMS);
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.COMMERCIAL, level, true, waitPeriodMS);
                await BuildingIPbyTypeGet((int)BUILDING_TYPE.MUNICIPAL, level, true, waitPeriodMS);
            }

            return RETURN_CODE.SUCCESS;
        }

        public async Task<RETURN_CODE> UpdateIPRankingByType(int type, int level, int waitPeriodMS = 100)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            try
            {
                await BuildingIPbyTypeGet(type, level, true, waitPeriodMS);

                _context.SaveChanges();

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("BuildingManage.UpdateIPRankingByType() : Error on updating IP ranking for all buildings matching a specific building type and level"));
                    _context.LogEvent(log);
                }
            }

            return returnCode;
        }

        // History Service call has a milisecond wait feature - to reduce overloading backend services (and risk block)
        public async Task<BuildingCollection> BuildingIPbyTypeGet(int buildingType, int buildingLevel, bool evalHistory, int waitPeriodMS = 100)
        {
            Building building = new();
            Dictionary<int, decimal> IPRank = new();
            List<BuildingTypeIP> buildingList = null;
            List<ResourceActive> activeBuildingList = new();
            int position = 1, correctAppBonus = 0, saveCounter = 0;

            try
            {
                BuildingTypeIPDB buildingTypeIPDB = new(_context);
                DistrictPerkDB districtPerkDB = new(_context);
                buildingCollection.show_prediction = Startup.showPrediction;

                List<DistrictPerk> districtPerkList = districtPerkDB.PerkGetAll_ByPerkType((int)DISTRICT_PERKS.EXTRA_SLOT_APPLIANCE_ALL_BUILDINGS);
                //MathNet.Numerics.Distributions.Normal distribution = new();

                buildingList = buildingTypeIPDB.BuildingTypeGet(buildingType, buildingLevel).ToList();

                if (buildingList.Count > 0)
                {
                    buildingCollection.maxIP = (int)Math.Round(buildingList.Max(x => x.influence * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);
                    buildingCollection.minIP = (int)Math.Round(buildingList.Where(x => x.influence > 0).Min(x => x.influence * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);

                    //buildingCollection.minIP = buildingCollection.minIP < 0 ? 0 : buildingCollection.minIP;     // Energy building could have negative IP from breakdown calc

                    buildingCollection.avgIP = Math.Round(buildingList.Average(x => x.influence * (1 + ((x.influence_bonus) / 100.0))), 0, MidpointRounding.AwayFromZero);

                    buildingCollection.rangeIP = buildingCollection.maxIP - buildingCollection.minIP;

                    // Calc and Eval Produce Prediction : using building that produced in last 7 days.
                    //buildingPredictList = buildingList.Where(x => x.last_run_produce_date >= new DateTime(2022, 01, 30, 23, 00, 00)).ToList();
                    //buildingPredictionSummary(buildingList);
                }

                buildingCollection.sync_date = common.TimeFormatStandard(string.Empty, _context.ActionTimeGet(ACTION_TYPE.PLOT));
                buildingCollection.building_type = GetBuildingTypeDesc(buildingType);
                buildingCollection.building_lvl = buildingLevel;
                buildingCollection.buildingIP_impact = (buildingType == (int)BUILDING_TYPE.RESIDENTIAL || buildingType == (int)BUILDING_TYPE.ENERGY) ? 20 : 40;
                buildingCollection.img_url = "https://play.mcp3d.com/assets/images/buildings/";
                List<ResourceTotal> resourceTotal = new();
                //List<ResourceTotal> resourceTotalExcess = new();

                for (int i = 0; i < buildingList.Count; i++)
                {
                    // Collate all recent produce 
                    UpdateResourceTotal(resourceTotal, buildingType, buildingList[i]);

                    buildingList[i].building_img = building.BuildingImg(buildingType, buildingList[i].building_id, buildingLevel).Replace(buildingCollection.img_url,"");

                    buildingList[i].influence_info = buildingList[i].influence_info < 0 ? 0 : buildingList[i].influence_info;
                    if (buildingList[i].influence_info != buildingList[i].influence)
                    {
                        // Visual bug caused by not including negative IP impact
                        buildingList[i].ip_warning = string.Concat("MCP Base IP UI bug : ", buildingList[i].influence_info, "[Correct] vs ", buildingList[i].influence, "[Incorrect in Plot History] ");
                    }

                    // CHECK if application perk is active - confirm IP is correct or bugged
                    DistrictPerk districtPerk = districtPerkList.Where(x => x.district_id == buildingList[i].district_id).FirstOrDefault();
                    if (districtPerk != null)
                    {
                        if (districtPerk.perk_level == 1) {
                            correctAppBonus = buildingList[i].app_123_bonus + buildingList[i].app_4_bonus;
                        }
                        else {
                            correctAppBonus = buildingList[i].app_123_bonus + buildingList[i].app_4_bonus + buildingList[i].app_5_bonus;
                        }

                        if (correctAppBonus != buildingList[i].influence_bonus) {
                            buildingList[i].ip_warning = string.Concat("Application bug : using app bonus ", buildingList[i].influence_bonus, "% versus correct value of ", correctAppBonus, "%,", districtPerk.perk_level == 1 ? " ONLY" : "", " Perk Extra Slot ", districtPerk.perk_level, " is active");
                        }

                    }

                    buildingList[i].total_ip = GetInfluenceTotal(buildingList[i].influence_info, buildingList[i].influence_bonus);
                    buildingList[i].ip_efficiency = (decimal)Math.Round(GetIPEfficiency(buildingList[i].total_ip, buildingCollection.rangeIP, buildingCollection.minIP) * 100, 2);

                    if (evalHistory && (buildingType == (int)BUILDING_TYPE.INDUSTRIAL || buildingType == (int)BUILDING_TYPE.PRODUCTION || buildingType == (int)BUILDING_TYPE.ENERGY)
                        && EvalBuildingHistory(buildingList[i].last_run_produce_date, buildingLevel) == true)
                    {
                        // Save each building's evaluated IP efficiency and predicted produce on next run.
                        await GetHistory(buildingList[i].token_id, waitPeriodMS, false, false, buildingList[i].owner_matic, buildingList[i].ip_efficiency);
                        await WaitPeriodAction(waitPeriodMS);       // Wait set period required reduce load on MCP services - min 100ms

                        saveCounter++;

                        // Save every 30 History evals - improve performance on local db updates.
                        if (saveCounter >= 30 || i == buildingList.Count - 1)
                        {
                            _context.SaveWithRetry();
                            saveCounter = 0;
                        }
                    }
                    else
                    {
                        //Store IP rank and record in bulk
                        IPRank.Add(buildingList[i].token_id, buildingList[i].ip_efficiency);
                    }
                }

                resourceTotal = resourceTotal.OrderBy(x => x.name).ToList();
                buildingCollection.total_produced = GetResourceTotalDisplay(resourceTotal, 1, buildingLevel);
                buildingCollection.total_produced_month = GetResourceTotalDisplay(resourceTotal, 4, buildingLevel);

                for (int index = 0; index < resourceTotal.Count; index++)
                {
                    activeBuildingList.Add(new ResourceActive()
                    {
                        name = resourceTotal[index].name,
                        total = resourceTotal[index].buildingCount,
                        active = resourceTotal[index].buildingActive,
                        shutdown = resourceTotal[index].buildingCount - resourceTotal[index].buildingActive

                    }
                    );
                }
                buildingCollection.active_buildings = activeBuildingList.ToArray();

                // Calc and Eval Produce Prediction : using building that produced in last 7 days.
                // Refresh building list data if history eval step was completed as building prediction data may have changed/updated.
                // Potential for PERF improvement here - Remove need to call expensive db sproc again
                if (evalHistory)
                {
                    buildingList = buildingTypeIPDB.BuildingTypeGet(buildingType, buildingLevel).ToList();
                }

                buildingPredictionSummary(buildingList);

                buildingList = buildingList.OrderByDescending(x => x.total_ip).ToList();

                for (int i = 0; i < buildingList.Count; i++)
                {
                    buildingList[i].position = position++;
                }

                // Store all IP for non evaluated buildings (no history check) - such as residential, municipals, office.
                if (IPRank.Count > 0)
                {
                    foreach (KeyValuePair<int, Decimal> plotRank in IPRank)
                    {
                        Plot plot = _context.plot.Where(x => x.token_id == plotRank.Key).FirstOrDefault();
                        plot.current_influence_rank = plotRank.Value;
                    }
                    _context.SaveChanges();
                }

                buildingCollection.buildings = ConvertBuildingWeb(buildingList);
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("BuildingManage.BuildingTypeGet() : Error on retrival of List matching buildingType: ", buildingType, "and level ", buildingLevel));
                    _context.LogEvent(log);
                }
            }

            return buildingCollection;
        }

        private IEnumerable<BuildingIPWeb> ConvertBuildingWeb(List<BuildingTypeIP> buildingList)
        {
            List<BuildingIPWeb> buildingIPWeb = new();

            foreach(BuildingTypeIP building in buildingList)
            {
                buildingIPWeb.Add(new()
                {
                    id = building.token_id,
                    dis = building.district_id,
                    pos_x = building.pos_x,
                    pos_y = building.pos_y,

                    pos = building.position,
                    rank = building.ip_efficiency,
                    ip_t = building.total_ip,                   // plot.influence_info * plot.influence_bonus
                    ip_b = building.influence_info,
                    bon = building.influence_bonus,
                    name = building.owner_nickname,
                    name_id = building.owner_avatar_id,
                    name_m = building.owner_matic,
                    con = building.condition,
                    act = building.active_building,
                    pre = building.predict_eval_result ?? 0,
                    warn = building.ip_warning ?? "",
                    img = building.building_img,
                    price = building.current_price,
                    r_p = building.for_rent,                    // rent price
                    ren = building.rented == true ? 1 : 0,      // rented
                    poi = building.production_poi_bonus

                });
            }

            return buildingIPWeb.ToArray();

        }

        public int buildingPredictionSummary(List<BuildingTypeIP> buildingList)
        {
            List<BuildingTypeIP> buildingPredictList = null;

            // Calc and Eval Produce Prediction : using building that produced in last 7 days.
            //buildingPredictList = buildingList.Where(x => x.last_run_produce_date >= new DateTime(2022, 01, 30, 23, 00, 00)).ToList();
            buildingCollection.active_count = buildingList.Where(x => x.last_run_produce_date >= DateTime.Now.AddDays(-7)).Count();
            buildingPredictList = buildingList.Where(x => x.last_run_produce_date >= DateTime.Now.AddDays(-7) && x.last_run_produce_predict == true).ToList();
            buildingCollection.buildings_predict = buildingPredictList.Count;
            buildingCollection.predict = new();

            if (buildingCollection.buildings_predict > 0)
            {
                // STANDARD Prediction eval
                buildingCollection.predict.correct = buildingPredictList.Count(x => x.predict_produce == x.last_run_produce);
                buildingCollection.predict.correct_percent = (decimal)Math.Round((decimal)buildingCollection.predict.correct / buildingCollection.buildings_predict, 4, MidpointRounding.AwayFromZero) * 100;

                buildingCollection.predict.miss = buildingCollection.buildings_predict - buildingCollection.predict.correct;
                buildingCollection.predict.miss_percent = (decimal)Math.Round((decimal)buildingCollection.predict.miss / buildingCollection.buildings_predict, 4, MidpointRounding.AwayFromZero) * 100;

                if (buildingCollection.predict.miss > 0)
                {
                    buildingCollection.predict.miss_above = buildingPredictList.Count(x => x.predict_produce > x.last_run_produce);
                    buildingCollection.predict.miss_above_percent = (decimal)Math.Round((decimal)buildingCollection.predict.miss_above / buildingCollection.predict.miss, 4, MidpointRounding.AwayFromZero) * 100;

                    buildingCollection.predict.miss_below = buildingCollection.predict.miss - buildingCollection.predict.miss_above;
                    buildingCollection.predict.miss_below_percent = (decimal)Math.Round((decimal)buildingCollection.predict.miss_below / buildingCollection.predict.miss, 4, MidpointRounding.AwayFromZero) * 100;
                }                

                for (int i = 0; i < buildingPredictList.Count; i++)
                {
                    buildingPredictList[i].predict_eval = true;
                    buildingPredictList[i].predict_eval_result = buildingPredictList[i].predict_produce - buildingPredictList[i].last_run_produce;
                }
            }

            return 0;
        }

        public int GetInfluenceTotal(int influence, int influenceBonus)
        {
            return (int)Math.Round(influence * (1 + (influenceBonus / 100.0)), 0, MidpointRounding.AwayFromZero);
        }

        // fullRefresh = true : true flag only used by user triggered refresh event from ProductionHistory module : updates Plot with latest MCP WS
        public async Task<BuildingHistory> GetHistory(int asset_id, int waitPeriodMS, bool forceDBSave, bool fullRefresh, string ownerMatic, decimal ipEfficiency = USE_STORED_EFFICIENCY)
        {            
            try
            {
                buildingHistory = new();
                string content = string.Empty;
                DateTime? eventTimeUAT, lastRunTime = null;
                int runInstanceCount = 0, actionCount = 0;
                int storedIp = 0;
                List<HistoryProduction> historyProductionList = new();
                List<ResourceTotal> resourceTotal = new();
                List<PlotIP> plotIPList = new();
                ResourceTotal currentResource = null;
                OwnerCitizenExtDB ownerCitizenExtDB = new(_context);                

                PlotDB plotDB = new PlotDB(_context);
                plotIPList = plotDB.GetIP_Historic(asset_id);
                List<Plot> buildingPlots = null;
                Plot targetPlot = null;
                DateTime? targetPlotLastUpdated = null;
                bool resourceFlag = false, applianceFlag = false;
                IEnumerable<string> storeChangesLastRunMsg;


                if (ipEfficiency == USE_STORED_EFFICIENCY)
                {
                    buildingPlots = plotDB.GetPlotbyToken(asset_id);
                    if (buildingPlots.Count() > 0)
                    {
                        targetPlot = buildingPlots[0];
                        targetPlotLastUpdated = buildingPlots[0].last_updated;          // Used to prevent spaming of this feature - only allow refresh every 10 mins per building
                        ipEfficiency = targetPlot.current_influence_rank ?? 0;
                        ownerMatic = targetPlot.owner_matic;
                        buildingHistory.current_building_id = targetPlot.building_id;

                        // User initiated Refresh due to stale data - damage, ip, citizens, poi bonus, apps
                        if (fullRefresh == true && targetPlotLastUpdated < DateTime.UtcNow.AddMinutes(-5))
                        {
                            // Check if IP has changed since last update (first building in collection is fine)
                            storedIp = GetInfluenceTotal(buildingPlots[0].influence_info ?? 0, buildingPlots[0].influence_bonus ?? 0);

                            for (int i = 0; i < buildingPlots.Count(); i++)
                            {
                                plotDB.AddOrUpdatePlot(buildingPlots[i].pos_x, buildingPlots[i].pos_y, buildingPlots[i].plot_id, false);
                            }
                            _context.SaveChanges();

                            buildingPlots = plotDB.GetPlotbyToken(asset_id);        // Refresh to get possibly updated IP / IP Bonus due to app change / POI change reactivated etc
                            
                            if (storedIp != GetInfluenceTotal(buildingPlots[0].influence_info ?? 0, buildingPlots[0].influence_bonus ?? 0)){
                                buildingHistory.changes_last_run = AddChangeMessage("REFRESH PAGE - IP change and Ranking Change Identified, all buildings need to be reevaluated", buildingHistory.changes_last_run);
                            }
                        }
                    }
                }

                buildingHistory.owner_matic = ownerMatic;

                HttpResponseMessage response;
                serviceUrl = "https://ws-tron.mcp3d.com/user/assets/history";
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"token_id\": " + asset_id + ",\"token_type\": 1}", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();

                }
                watch.Stop();
                servicePerfDB.AddServiceEntry("Building - " + serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, asset_id.ToString());

                JArray historyItems = JArray.Parse(content);
                if (historyItems != null && historyItems.Count > 0)
                {
                    ownerCitizenExt = ownerCitizenExtDB.GetBuildingCitizen(asset_id);

                    for (int index = 0; index < historyItems.Count; index++)
                    {
                        JToken historyItem = historyItems[index];
                        resourceFlag = (historyItem.Value<string>("type") ?? "").Equals("management/resource_produced");
                        applianceFlag = (historyItem.Value<string>("type") ?? "").Equals("management/appliance_produced");

                        // Check if building history relates to current owner (ignore all prior owners history)
                        if ((resourceFlag || applianceFlag) &&
                             (historyItem.Value<string>("to_address") ?? "").ToUpper().Equals(buildingHistory.owner_matic.ToUpper()))
                        {
                            HistoryProduction historyProductionDetails = new();
                            eventTimeUAT = historyItem.Value<DateTime?>("event_time");
                            lastRunTime ??= eventTimeUAT;

                            historyProductionDetails.run_datetime = common.DateStandard(common.TimeFormatStandardDT("", historyItem.Value<DateTime?>("event_time")));
                            historyProductionDetails.run_datetimeDT = ((DateTime)eventTimeUAT).Ticks;

                            runInstanceCount++;

                            JToken historyData = historyItem.Value<JToken>("data");
                            if (historyData != null && historyData.HasValues)
                            {
                                if (resourceFlag)
                                {
                                    historyProductionDetails.amount_produced = historyData.Value<int?>("amount") ?? 0;
                                    historyProductionDetails.building_product_id = historyData.Value<int?>("resourceId") ?? 0;
                                    historyProductionDetails.building_product = GetResourceName(historyProductionDetails.building_product_id);
                                }
                                else // Appliance
                                {
                                    historyProductionDetails.amount_produced = 1;
                                    historyProductionDetails.building_product_id = historyData.Value<int?>("typeId") ?? 0;
                                    historyProductionDetails.building_product = GetApplianceName(historyProductionDetails.building_product_id);
                                }

                                // Find Stored Resource match, and increment
                                if (resourceTotal.Count > 0)
                                {
                                    currentResource = (ResourceTotal)resourceTotal.Where(row => row.resourceId == historyProductionDetails.building_product_id).FirstOrDefault();
                                }
                                if (currentResource == null)
                                {
                                    currentResource = new();
                                    currentResource.resourceId = historyProductionDetails.building_product_id;
                                    currentResource.name = historyProductionDetails.building_product;
                                    resourceTotal.Add(currentResource);
                                }

                                currentResource.total += historyProductionDetails.amount_produced;

                                // CALC and ASSIGN Citizen Eff % fields
                                JToken historyLand = historyData.Value<JToken>("land");
                                if (historyLand != null && historyLand.HasValues)
                                {
                                    historyProductionDetails = PopulateProductionDetails(historyProductionDetails,
                                        historyLand.Value<int?>("building_type_id") ?? 0,
                                        historyLand.Value<int?>("building_level") ?? 0,
                                        eventTimeUAT);

                                    var matchedIP = plotIPList.Where(x => x.last_updated <= (DateTime)eventTimeUAT).OrderByDescending(x => x.last_updated).FirstOrDefault();
                                    historyProductionDetails.building_ip = matchedIP == null ? 0 : matchedIP.total_ip;
                                    historyProductionDetails.influence_bonus = matchedIP == null ? 0 : matchedIP.influence_bonus ?? 0;

                                    historyProductionDetails.poi_bonus = matchedIP == null || matchedIP.production_poi_bonus == null ? 0 : (decimal)matchedIP.production_poi_bonus;
                                    //historyProductionDetails.is_perk_activated = matchedIP == null ? false : matchedIP.is_perk_activated ?? false;
                                }
                            }

                            historyProductionList.Add(historyProductionDetails);
                        }
                    }

                    // TO_DO - Need to support  predictions with no prior history
                    if (historyProductionList.Count > 0)
                    {
                        buildingHistory.run_count = runInstanceCount;
                        buildingHistory.start_production = historyProductionList.Last().run_datetime;
                        buildingHistory.totalProduced = GetResourceTotalDisplay(resourceTotal, 1, 1);
                        buildingHistory.detail = historyProductionList.ToArray<HistoryProduction>();


                        // Calculate IP efficiency and prediction if data sent within method request
                        // Note
                        //      Use building level as recorded in last sync, not last run - as building upgrade may have occured.
                        //      historyProductionList[0] contains buidlings currently assigned citizens - for the current IN-PROGRESS run, used for eval of current run.
                        if (ipEfficiency > -1)
                        {
                            // REFRESH - CHECK citizen actions from now back to last production run, if citizen from last run has been removed (identified in history) then get all account cits again and reeval
                            if (fullRefresh == true && targetPlot != null && targetPlotLastUpdated < DateTime.UtcNow.AddMinutes(-5))
                            {
                                historyProductionList[0] = FullRecheckCitizens((DateTime)lastRunTime, asset_id, waitPeriodMS, targetPlot, historyProductionList[0]);                                
                            }

                            // As Prediction may be reevaluated need to store any prior warnings and readd them (otherwise lost in rerun).
                            storeChangesLastRunMsg = buildingHistory.changes_last_run;  

                            // Main Prediction eval call
                            buildingHistory = GetPrediction(buildingHistory, historyProductionList[0], asset_id, ipEfficiency, lastRunTime);

                            // RE-EVAL CASES : CHECK if citizen history needs to be evaluated for add/removal of pets, then get the pet change events and redo GetPrediction calc
                            if (buildingHistory.check_citizens == true)
                            {
                                // Find any missing Citizen events since last production run and save to db.                                
                                actionCount = CheckBuildingCitizenHistory(ownerCitizenExt, (DateTime)lastRunTime, asset_id, buildingHistory.owner_matic, waitPeriodMS);

                                // PERF - if no changes found then dont continue re-eval
                                if (actionCount > 0)
                                {
                                    // Refresh Citizen list for this building due to prior step actions
                                    ownerCitizenExt = ownerCitizenExtDB.GetBuildingCitizen(asset_id);

                                    // Reeval Prediction with updated OwnerCitizen's,  need to also reeval last production run - pulling in any missing pet usage added in last step.
                                    // Recalculate Citizen Eff% with pets on the last run.
                                    historyProductionList[0] = PopulateProductionDetails(historyProductionList[0], historyProductionList[0].building_type, historyProductionList[0].building_lvl, new DateTime(historyProductionList[0].run_datetimeDT));

                                    buildingHistory = GetPrediction(buildingHistory, historyProductionList[0], asset_id, ipEfficiency, lastRunTime);
                                }
                            }

                            // Add Stored Change messages if any
                            if (storeChangesLastRunMsg != null && storeChangesLastRunMsg.Any())
                            {
                                if (buildingHistory.changes_last_run == null)
                                {
                                    buildingHistory.changes_last_run = storeChangesLastRunMsg.ToArray();
                                }
                                else
                                {
                                    List<string> tempList = buildingHistory.changes_last_run.ToList();
                                    buildingHistory.changes_last_run = storeChangesLastRunMsg.Concat(tempList).ToArray();
                                }
                            }

                            // Save IP efficiency data for this building (set of plots), prediction produce total and last run only applicable to Ind & Prod & Eng, when no major change found used by "Predict Eval" feature.
                            plotDB.UpdatePlot(
                                asset_id,
                                ipEfficiency,
                                0,
                                buildingHistory.prediction.total,
                                0, //buildingHistory.prediction_bonus_bug != null ? buildingHistory.prediction_bonus_bug.total : buildingHistory.prediction.total,
                                historyProductionList[0].amount_produced,
                                historyProductionList[0].building_product_id,
                                lastRunTime,
                                buildingHistory.use_prediction,
                                forceDBSave);
                        }
                    }
                    else
                    {
                        // Default clear any prior production run data for this building and set efficiency fields, may occur due to transfer/sale. no need to force a save.
                        plotDB.UpdatePlot(
                            asset_id,
                            ipEfficiency,
                            0,
                            0,
                            0,
                            0,
                            0,
                            null,
                            false,
                            forceDBSave);
                    }
                }

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("AssetHistoryController.GetHistory() : Error on WS calls for asset id : ", asset_id));
                    _context.LogEvent(log);
                }
            }

            return buildingHistory;
        }

        public HistoryProduction FullRecheckCitizens(DateTime lastRunTime, int asset_id, int waitPeriodMS, Plot targetPlot, HistoryProduction historyProduction)
        {
            int lastRunActionCount = 0;
            int currentCitizensActionCount = 0;
            Boolean citizenChange = false;
            OwnerCitizenExtDB ownerCitizenExtDB = new(_context);

            // A: Check Citizens assigned during last run.
            lastRunActionCount = CheckBuildingCitizenHistory(ownerCitizenExt, lastRunTime, asset_id, buildingHistory.owner_matic, waitPeriodMS);

            List<OwnerCitizenExt> lastRunCitizens = ownerCitizenExt.Where(x => (x.valid_to_date >= lastRunTime || x.valid_to_date is null) && x.link_date < lastRunTime).ToList();
            List<OwnerCitizenExt> currentCitizens = ownerCitizenExt.Where(x => x.valid_to_date is null).ToList();

            // B: SPECIAL CASE L User is repeat testing different cits - may have full set but one or more are different, previously updated full account cits so current cits will show full set.
            if (currentCitizens.Count == lastRunCitizens.Count)
            {
                foreach (OwnerCitizenExt lastRunCit in lastRunCitizens)
                {
                    if (currentCitizens.Where(x => x.token_id == lastRunCit.token_id).Count() == 0)
                    {
                        citizenChange = true;
                        break;
                    }                    
                }
            }

            // C: Check if citizens have been swapped out since last nightly run
            // if current building cit count differs then last run - or cits swaped out
            // Example: current cit count will drop if a cit was swapped out in last 24hrs - picked up by prior check on Citizen action changes call CheckBuildingCitizenHistory() on local db 
            if (currentCitizens.Count != lastRunCitizens.Count || citizenChange == true)
            {
                buildingHistory.changes_last_run = AddChangeMessage("Citizen swap change identified since last run", buildingHistory.changes_last_run);

                // Refresh all Citizens for this owner - get lastest from MCP
                CitizenManage citizenManage = new(_context);
                citizenManage.GetCitizenMCP(targetPlot.owner_matic).Wait();

                OwnerCitizenDB ownerCitizenDB = new(_context);
                ownerCitizenDB.UpdateCitizenCount();

                // Find latest citizens used by building.
                ownerCitizenExt = ownerCitizenExtDB.GetBuildingCitizen(asset_id);
                currentCitizens = ownerCitizenExt.Where(x => x.valid_to_date is null).ToList();
            }

            // Filter latest set of current cits assigned, remove any previously assigned - as already checked for pets.
            for (int i = 0; i<lastRunCitizens.Count; i++)
            {
                currentCitizens = currentCitizens.Where(x => x.token_id != lastRunCitizens[i].token_id).ToList();
            }

            // Check Citizens currently assigned - may have had temp pets assigned currently - only include cits not already checked (last run)
            if (currentCitizens.Count > 0)
            {
                currentCitizensActionCount = CheckBuildingCitizenHistory(currentCitizens, DateTime.Now, asset_id, buildingHistory.owner_matic, waitPeriodMS);
            }

            if (lastRunActionCount > 0 || currentCitizensActionCount > 0)
            {
                ownerCitizenExt = ownerCitizenExtDB.GetBuildingCitizen(asset_id);
            }

            // Citizens change found during prior run.
            if (lastRunActionCount > 0)
            {
                // Refresh Citizen list for this building due to prior step actions                                    
                historyProduction = PopulateProductionDetails(historyProduction, historyProduction.building_type, historyProduction.building_lvl, new DateTime(historyProduction.run_datetimeDT));
            }

            return historyProduction;
        }

        public HistoryProduction PopulateProductionDetails(HistoryProduction historyProductionDetails, int buildingTypeId, int buildingLevel, DateTime? eventTime)
        {
            if (eventTime != null)
            {
                eventTime = ((DateTime)eventTime).AddSeconds(-(int)CITIZEN_HISTORY.CORRECTION_SECONDS);  // Assign/Remove Pets may occur before event date is assigned to production run and still be included in run
            }

            historyProductionDetails.building_type = buildingTypeId;
            historyProductionDetails.building_lvl = buildingLevel;

            historyProductionDetails.efficiency_p = CalculateEfficiency_Production(historyProductionDetails.building_type, historyProductionDetails.building_lvl, historyProductionDetails.amount_produced, historyProductionDetails.building_product_id);
            historyProductionDetails.efficiency_m = CalculateEfficiency_MinMax(historyProductionDetails.building_type, historyProductionDetails.building_lvl, historyProductionDetails.amount_produced, historyProductionDetails.building_product_id);

            historyProductionDetails.efficiency_c_60 = CalculateEfficiency_Citizen(
                ownerCitizenExt,
                eventTime,
                historyProductionDetails.building_lvl, historyProductionDetails.building_type,
                historyProductionDetails.building_product_id,
                null);

            historyProductionDetails.pet_usage = citizenManage.GetPetUsage(eventTime, ownerCitizenExt);

            historyProductionDetails.efficiency_c = Math.Round(
                historyProductionDetails.efficiency_c_60
                / (historyProductionDetails.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? .45m : .6m),
                1,
                MidpointRounding.AwayFromZero);

            return historyProductionDetails;
        }

        public BuildingHistory GetPrediction(BuildingHistory buildingHistory, HistoryProduction lastProduction, int asset_id, decimal ipEfficiency, DateTime? lastRunTime)
        {
            List<string> changeSinceLastRun = new();
            buildingHistory.changes_last_run = null;
            buildingHistory.use_prediction = false;
            buildingHistory.check_citizens = false;

            PlotDB plotDB = new PlotDB(_context);
            List<Plot> targetPlotList = plotDB.GetPlotbyToken(asset_id);   // MEGA & HUGE return multiple plots.
            Plot targetPlot = targetPlotList[0];
            int targetPlotLevel = targetPlot.building_level;

            //CHECK if pets used in last prod run - then include in predicted next run. Pets may have been removed from cits - temp use is common
            PetUsage petUsageCurrentCits = citizenManage.GetPetUsage(DateTime.Now, ownerCitizenExt);

            buildingHistory.damage = 100 - (targetPlot.condition ?? 0);
            buildingHistory.damage_eff = GetDamageCoeff(buildingHistory.damage);
            buildingHistory.damage_eff_rounded = Math.Round(buildingHistory.damage_eff - 100, 2, MidpointRounding.AwayFromZero);
            //buildingHistory.damage_partial = Math.Round(buildingHistory.damage * buildingHistory.damage_eff, 2, MidpointRounding.AwayFromZero);

            buildingHistory.current_building_product_id = GetBuildingProduce(targetPlot.building_type_id, targetPlot.building_id, targetPlot.action_id);
            buildingHistory.prediction_product = GetResourceName(buildingHistory.current_building_product_id);

            buildingHistory.prediction_base_min = GetBaseProduce(lastProduction.building_type, targetPlotLevel, buildingHistory.current_building_product_id);
            buildingHistory.prediction_max = GetMaxProduce(lastProduction.building_type, targetPlotLevel, buildingHistory.current_building_product_id);
            buildingHistory.prediction_range = buildingHistory.prediction_max - buildingHistory.prediction_base_min;
            buildingHistory.current_building_lvl = targetPlotLevel;

            // prediction.prediction_ip_doublebug = (int)Math.Round((decimal)targetPlot.influence * (decimal)(1 + ((evalIPbonus *2) / 100.0)), 0, MidpointRounding.AwayFromZero);
            // Standard Prediction evaluation
            Prediction prediction = GetPredictionData(targetPlotList, lastProduction, buildingHistory, ipEfficiency);
            if (buildingHistory.changes_last_run != null)
            {
                changeSinceLastRun.AddRange(buildingHistory.changes_last_run);
            }

            if (prediction.cit_efficiency == 0)
            {
                prediction.total = 0;
                prediction.total_decimal = 0;
                prediction.total_note = "No Cit assigned";
            }

            // Check if citizen uses temp pets, if prediction is lower then actual last run & Building not upgraded & IP unchanged & POI Bonus unchanged
            // Example Ispera or Returner : X282 Y70,  add & remove pet per run - within 4 mins period, need to check each cit history and insert db records so history can calcuate Cit eff% correctly per run.
            if (targetPlotLevel == lastProduction.building_lvl &&
                lastProduction.building_ip == prediction.ip &&
                lastProduction.poi_bonus == prediction.poi_bonus)
            {
                changeSinceLastRun.AddRange(GetPetChanges(petUsageCurrentCits, lastProduction.pet_usage));
            }

            //if (new DateTime(lastProduction.run_datetimeDT) < DateTime.Now.AddDays(-7))
            //{
            //    changeSinceLastRun.Add("Last run > 7 days ago, wont be included in Prediction eval due to age");
            //}

            buildingHistory.prediction = prediction;

            // Dont save last run amount_produced to plot db if   (this plot will then not be included in the prediction eval feature) 
            // 1) building was upgraded since last run - as this last produce value is used in IP league prediction eval.
            // 2) double produce occured due to staked perk, dont include in prediction eval
            // 3) double produce runs might not exceed max produce due to low cit and IP eff%, so remove outliers where produce is >50% predicted.
            // 4) predicted Citizen efficiency is 0% - meaning no cits currently assigned.
            // 5) Citizen changed from last run (Cit efficiency change), future run & prediction will change to reflect current cits assigned - dont use in eval if diff.
            // 6) If IP used by Prediction differs from last run IP, then dont use in eval.
            if (targetPlotLevel == lastProduction.building_lvl &&
                lastProduction.building_ip == prediction.ip &&
                lastProduction.poi_bonus == prediction.poi_bonus &&
                prediction.cit_efficiency_rounded != 0 &&
                lastProduction.building_product_id == buildingHistory.current_building_product_id &&
                lastProduction.amount_produced <= buildingHistory.prediction_max &&
                lastProduction.amount_produced < prediction.total * 1.6)
            {
                // Prediction is valid to use for eval
                if (prediction.cit_efficiency_rounded == (int)Math.Round(lastProduction.efficiency_c_60, 0, MidpointRounding.AwayFromZero))
                {
                    buildingHistory.use_prediction = true;

                    // CHECK if 2x or 4x app bonus bug is active.
                    //if ((targetPlotLevel == 6 || targetPlotLevel == 7) && lastProduction.amount_produced != prediction.total)
                    //{
                    //    buildingHistory.prediction_bonus_bug = GetPredictionData(targetPlotList, lastProduction, buildingHistory, ipEfficiencyBonusAppBug);
                    //    buildingHistory.prediction_bonus_bug.ip = (int)Math.Round((decimal)targetPlot.influence *
                    //        (decimal)(1 + (
                    //        (buildingHistory.prediction_bonus_bug.influance_bonus * (targetPlotLevel == 6 ? 2 : 4))
                    //        / 100.0)), 0, MidpointRounding.AwayFromZero);

                    //if (buildingHistory.prediction_bonus_bug.total == lastProduction.amount_produced)
                    //{
                    //    changeSinceLastRun.Add(string.Concat(targetPlotLevel == 6 ? "2x" : "4x", " Application bonus bug active!"));
                    //    changeSinceLastRun.Add("Prediction matches last run produce when application bonus bug applied.");
                    //}
                    //}

                    if (!changeSinceLastRun.Any())
                    {
                        changeSinceLastRun.Add("None found");
                    }
                }
                else
                {
                    buildingHistory.check_citizens = true;          // Redo prediction process after retrival of updated citizen and pet use
                }
            }
            else
            {
                // Prediction doesnt match last run - find reasons why.
                buildingHistory.use_prediction = false;

                if (prediction.cit_efficiency != (decimal)Math.Round(lastProduction.efficiency_c, 1, MidpointRounding.AwayFromZero))
                {
                    decimal diff = prediction.cit_efficiency - lastProduction.efficiency_c;   // Show diff out of 100% for easy compare with last run
                    changeSinceLastRun.Add(string.Concat("Citizens change identified : ", (diff > 0 ? "+" : ""), diff, "%", prediction.cit_efficiency_rounded == 0 ? " (No Citizens Assigned)" : ""));
                }

                if (lastProduction.building_ip != prediction.ip)
                {
                    int diff = prediction.ip - lastProduction.building_ip;
                    changeSinceLastRun.Add("IP change identified : " + (diff > 0 ? "+" : "") + diff);
                }
                if (lastProduction.poi_bonus != prediction.poi_bonus)
                {
                    decimal diff = prediction.poi_bonus - lastProduction.poi_bonus;
                    changeSinceLastRun.Add("POI/Monument Bonus change identified : " + (diff > 0 ? "+" : "") + diff + "%");
                }
                //if (lastProduction.is_perk_activated != prediction.is_perk_activated)
                //{
                //    changeSinceLastRun.Add(string.Concat("Application Perk change identified : Currently ", prediction.is_perk_activated ? "Active" : "Inactive", ", last run ", lastProduction.is_perk_activated ? "Active" : "Inactive"));
                //}
                if (lastProduction.building_product_id == buildingHistory.current_building_product_id && lastProduction.amount_produced > buildingHistory.prediction_max)
                {
                    changeSinceLastRun.Add(string.Concat("Double Produce Perk identified : occuring on last run"));
                }
                if (lastProduction.building_product_id != buildingHistory.current_building_product_id)
                {
                    changeSinceLastRun.Add(string.Concat("Production type changed since last run : ", buildingHistory.prediction_product, " vs ", lastProduction.building_product));
                }
                if (targetPlotLevel != lastProduction.building_lvl)
                {
                    changeSinceLastRun.Add(string.Concat("Building has been upgraded (Lvl", lastProduction.building_lvl, " to Lvl", targetPlotLevel, ") since last run"));
                }

                //if (targetPlotLevel == 7)
                //{
                //    _context.LogEvent(String.Concat(asset_id, " Building Prediction Fail > ", String.Join(" :: ", changeSinceLastRun.ToArray())));
                //}
            }
            buildingHistory.changes_last_run = changeSinceLastRun.ToArray();

            return buildingHistory;
        }

        Prediction GetPredictionData(List<Plot> targetPlotList, HistoryProduction lastProduction, BuildingHistory buildingHistory, decimal ipEfficiency)
        {
            Prediction prediction = new();
            Plot targetPlot = targetPlotList[0];
            int evalIPbonus = targetPlot.influence_bonus ?? 0;  //(targetPlot.app_123_bonus + ((targetPlot.is_perk_activated ?? false) ? (targetPlot.app_4_bonus ?? 0) + (targetPlot.app_5_bonus ?? 0) : 0));
            int buildingType = targetPlot.building_type_id;

            prediction.influance = targetPlot.influence ?? 0;
            prediction.influance_bonus = evalIPbonus;

            // USE influence_info as this is more accurate using calculation of all building influance, rather then the building.influence with may be stale showing prior influance when POI/Mon is deactivated
            prediction.ip = (int)Math.Round((decimal)targetPlot.influence_info * (decimal)(1 + (evalIPbonus / 100.0)), 0, MidpointRounding.AwayFromZero);
            //prediction.is_perk_activated = targetPlot.is_perk_activated ?? false; // NOTE THIS ATTRIBUTE IS NOT WORKING (not filled by MCP) - CAN REMOVE LATER

            // Get Cit efficiency using currently assigned cits (may differ from last run) before produce calc                            
            prediction.cit_efficiency_partial = CalculateEfficiency_Citizen(ownerCitizenExt, DateTime.Now, targetPlot.building_level, lastProduction.building_type, lastProduction.building_product_id, lastProduction.pet_usage);
            prediction.cit_range_percent = lastProduction.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? 45 : 60;
            prediction.cit_efficiency = Math.Round(
                        prediction.cit_efficiency_partial
                            / (prediction.cit_range_percent / 100m),
                            1,
                            MidpointRounding.AwayFromZero);

            prediction.cit_efficiency_rounded = Math.Round(prediction.cit_efficiency_partial, 0, MidpointRounding.AwayFromZero);
            //buildingHistory.prediction_cit_produce = (buildingHistory.prediction_cit_efficiency_rounded / 100.0m) * buildingHistory.prediction_range;
            prediction.cit_produce = (prediction.cit_efficiency_rounded / 100.0m) * buildingHistory.prediction_range;
            prediction.cit_produce_rounded = Math.Round(prediction.cit_produce, 1, MidpointRounding.AwayFromZero);
            prediction.cit_produce_max = (prediction.cit_range_percent / 100.0m) * buildingHistory.prediction_range;


            // Energy plots - resource prediction calc
            if (targetPlot.abundance > 0 && buildingType == (int)BUILDING_TYPE.ENERGY)
            {
                int targetPlotAbundance = (int)Math.Round((decimal)((targetPlotList.Sum(x => x.abundance) ?? 0) / (decimal)targetPlotList.Count), 0, MidpointRounding.AwayFromZero);
                // Bug/Reward All Energy MEGA get a bump to next level of abundance.
                if (targetPlot.building_level == 7)
                {
                    buildingHistory.changes_last_run = new string[1]{
                       string.Concat("Bug/Feature : All Mega Energy Buildings receive +1 Resource lvl (", string.Join(",",targetPlotList.Select(x => x.abundance).ToArray()), ") = ", targetPlotAbundance, " +1 ")
                    };

                    targetPlotAbundance++;
                }

                prediction.resource_range_percent = lastProduction.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? 25 : 20;
                prediction.resource_lvl = targetPlotAbundance;
                prediction.resource_lvl_percent = lastProduction.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? GetElectricResouceLevel(prediction.resource_lvl) : GetWaterResouceLevel(prediction.resource_lvl);
                prediction.resource_partial = (prediction.resource_range_percent / 100.0m) * prediction.resource_lvl_percent;


                prediction.resource_lvl_range = buildingHistory.prediction_range * (prediction.resource_range_percent / 100m);
                prediction.resource_lvl_produce = (decimal)(buildingHistory.prediction_range *
                    (prediction.resource_range_percent / 100m) *
                    (prediction.resource_lvl_percent / 100.0m));

                prediction.resource_lvl_produce_rounded = (int)Math.Round(prediction.resource_lvl_produce, 0, MidpointRounding.AwayFromZero);
            }

            // Water : 20% , ENERGY : 30%, REST : 40%
            prediction.ip_range_percent = lastProduction.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? 30 : lastProduction.building_product_id == (int)BUILDING_PRODUCT.WATER ? 20 : 40;
            prediction.ip_efficiency = ipEfficiency;
            prediction.ip_efficiency_partial = (prediction.ip_efficiency / 100.0m) * prediction.ip_range_percent;
            prediction.ip_efficiency_rounded = (int)Math.Round(prediction.ip_efficiency_partial, 0, MidpointRounding.AwayFromZero);

            prediction.ip_produce = (prediction.ip_efficiency / 100.0m) * (prediction.ip_range_percent / 100m) * buildingHistory.prediction_range;
            prediction.ip_produce_rounded = Math.Round(prediction.ip_produce, 2, MidpointRounding.AwayFromZero);
            prediction.ip_produce_max = (prediction.ip_range_percent / 100.0m) * buildingHistory.prediction_range;

            // GOLDEN Rule: COMBINE IP and Cit % , then round to 0 places, then multiple against effective range.
            prediction.ip_and_cit_percent = prediction.ip_efficiency_partial + prediction.cit_efficiency_partial + prediction.resource_partial - buildingHistory.damage_partial;
            prediction.ip_and_cit_percent_100 = prediction.ip_efficiency_partial + prediction.cit_efficiency_partial + prediction.resource_partial;
            prediction.ip_and_cit_percent_rounded = Math.Round(prediction.ip_and_cit_percent, 0, MidpointRounding.AwayFromZero);
            prediction.ip_and_cit_produce = (prediction.ip_and_cit_percent_rounded / 100.0m) * buildingHistory.prediction_range;
            prediction.ip_and_cit_produce_rounded = (int)Math.Round(prediction.ip_and_cit_produce, 2, MidpointRounding.AwayFromZero);

            prediction.ip_and_cit_percent_dmg = Math.Round(prediction.ip_and_cit_percent_100 * (buildingHistory.damage_eff / 100m), 2, MidpointRounding.AwayFromZero);
            prediction.ip_and_cit_percent_rounded_dmg = (int)Math.Round(prediction.ip_and_cit_percent_dmg, 0, MidpointRounding.AwayFromZero);
            prediction.ip_and_cit_produce_dmg = (prediction.ip_and_cit_percent_rounded_dmg / 100.0m) * buildingHistory.prediction_range;
            prediction.ip_and_cit_produce_dmg_rounded = (int)Math.Round(prediction.ip_and_cit_produce_dmg, 0, MidpointRounding.AwayFromZero);

            prediction.subtotal = buildingHistory.prediction_base_min + prediction.ip_and_cit_produce_dmg_rounded;
            prediction.subtotal_rounded = (int)Math.Round(prediction.subtotal, 0, MidpointRounding.AwayFromZero);

            prediction.poi_bonus = targetPlot.production_poi_bonus; // Using current-latest-active POI bonus

            prediction.poi_bonus_produce = prediction.subtotal_rounded * (prediction.poi_bonus / 100.0m);
            prediction.poi_bonus_produce_rounded = (int)Math.Round(prediction.poi_bonus_produce, 0, MidpointRounding.AwayFromZero);


            prediction.total_decimal = Math.Round(
                    prediction.subtotal +
                            prediction.poi_bonus_produce,
                            2, MidpointRounding.AwayFromZero);

            prediction.total = (int)Math.Round(
                    prediction.subtotal +
                    prediction.poi_bonus_produce_rounded,
                    0, MidpointRounding.AwayFromZero);

            var ipCitProduce100Rounded = (int)Math.Round((Math.Round(prediction.ip_and_cit_percent_100, 0, MidpointRounding.AwayFromZero) / 100.0m) * buildingHistory.prediction_range, 0, MidpointRounding.AwayFromZero);
            prediction.total_decimal_100 = (buildingHistory.prediction_base_min + ipCitProduce100Rounded) * (1 + (prediction.poi_bonus / 100.0m));
            prediction.total_100 = (int)Math.Round(
                    prediction.total_decimal_100,
                    0, MidpointRounding.AwayFromZero);

            prediction.total_decimal_100 = Math.Round(prediction.total_decimal_100, 2, MidpointRounding.AwayFromZero);   // For UI only


            // Corner Cases (a)cant be higher then max - bonus may place higher calc (b)no cits assigned
            prediction.total = prediction.total > buildingHistory.prediction_max ? buildingHistory.prediction_max : prediction.total;

            return prediction;
        }

        private RETURN_CODE UpdateResourceTotal(List<ResourceTotal> resourceTotal, int buildingType, BuildingTypeIP building)
        {
            ResourceTotal currentResource = null;
            int produce = building.building_id switch
            {
                (int)BUILDING_SUBTYPE.FACTORY => (int)BUILDING_PRODUCT.FACTORY_PRODUCT,
                (int)BUILDING_SUBTYPE.BRICKWORKS => (int)BUILDING_PRODUCT.BRICK,
                (int)BUILDING_SUBTYPE.GLASSWORKS => (int)BUILDING_PRODUCT.GLASS,
                (int)BUILDING_SUBTYPE.CONCRETE_PLANT => (int)BUILDING_PRODUCT.CONCRETE,
                //(int)BUILDING_SUBTYPE.STEEL_PLANT => (int)BUILDING_PRODUCT.STEEL,                    
                _ => building.last_run_produce_id
            };

            if (CheckProduct(buildingType, produce, building.building_id))
            {
                // Find Stored Resource match, and increment
                if (resourceTotal.Count > 0)
                {
                    currentResource = (ResourceTotal)resourceTotal.Where(row => row.resourceId == produce).FirstOrDefault();
                }      
                if (currentResource == null)
                {
                    currentResource = new();
                    currentResource.resourceId = produce;
                    currentResource.name = produce> 0 ? GetResourceName(produce) : "No History";
                    resourceTotal.Add(currentResource);
                }

                // ACTIVE BUILDING (Last 7 days produced) - Add building last run produce details - including if active or shutdown
                if (building.last_run_produce != null && building.last_run_produce > 0 && building.last_run_produce_date >= DateTime.Now.AddDays(-7))
                {
                    currentResource.total += (long)building.last_run_produce;
                    currentResource.buildingCount++;
                    currentResource.buildingActive++;
                    building.active_building = true;
                }
                else
                {
                    currentResource.buildingCount++;
                    building.active_building = false;
                }
            }

            return RETURN_CODE.SUCCESS;
        }

        private bool CheckProduct(int buildingType, int produceId, int buildingId)
        {
            if (buildingType == (int)BUILDING_TYPE.PRODUCTION &&
               (produceId == (int)BUILDING_PRODUCT.BRICK
                 || produceId == (int)BUILDING_PRODUCT.CONCRETE
                 || produceId == (int)BUILDING_PRODUCT.GLASS
                 || buildingId == (int)BUILDING_SUBTYPE.FACTORY))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.INDUSTRIAL &&
                (produceId == (int)BUILDING_PRODUCT.SAND ||
                  produceId == (int)BUILDING_PRODUCT.STONE ||
                  produceId == (int)BUILDING_PRODUCT.WOOD ||
                  produceId == (int)BUILDING_PRODUCT.METAL
                ))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.ENERGY &&
                (produceId == (int)BUILDING_PRODUCT.ENERGY ||
                  produceId == (int)BUILDING_PRODUCT.WATER))
            {
                return true;
            }

            return false;
        }

        public decimal GetDamageCoeff(int damage)
        {
            decimal damageCoeff = 100;

            // https://www.dcode.fr/function-equation-finder
            // Using Parabola / Hyperbola using curve fitting
            // f(x) = −0.00773426x2 − 0.419696x + 100.215 
            if (damage > 0)
            {
                damageCoeff = 0.00773426m * (decimal)(damage * damage);
                damageCoeff += 0.419696m * damage;
                damageCoeff = 100.215m - damageCoeff;
            }

            return damageCoeff;

            /* return damage switch
            {
                <= 5 => (int)DAMAGE_EFF.DMG_1_TO_5 / 100m,
                <= 20 => (int)DAMAGE_EFF.DMG_1_TO_20 / 100m,
                <= 35 => (int)DAMAGE_EFF.DMG_1_TO_35 / 100m,
                <= 50 => (int)DAMAGE_EFF.DMG_1_TO_50 / 100m,
                <= 65 => (int)DAMAGE_EFF.DMG_1_TO_65 / 100m,
                <= 80 => (int)DAMAGE_EFF.DMG_1_TO_80 / 100m,
                <= 90 => (int)DAMAGE_EFF.DMG_1_TO_90 / 100m,
                _ => 0
            }; */
        }

        public List<string> GetPetChanges(PetUsage predictionPetUsage, PetUsage lastRunPetUsage)
        {
            List<string> changeSinceLastRun = new();

            changeSinceLastRun.Add(ComparePetUsage("Agility", lastRunPetUsage.agility, predictionPetUsage.agility));
            changeSinceLastRun.Add(ComparePetUsage("Charisma", lastRunPetUsage.charisma, predictionPetUsage.charisma));
            changeSinceLastRun.Add(ComparePetUsage("Endurance", lastRunPetUsage.endurance, predictionPetUsage.endurance));
            changeSinceLastRun.Add(ComparePetUsage("Intelligence", lastRunPetUsage.intelligence, predictionPetUsage.intelligence));
            changeSinceLastRun.Add(ComparePetUsage("Luck", lastRunPetUsage.luck, predictionPetUsage.luck));
            changeSinceLastRun.Add(ComparePetUsage("Strength", lastRunPetUsage.strength, predictionPetUsage.strength));

            changeSinceLastRun.RemoveAll(x => x == string.Empty);

            return changeSinceLastRun;
        }

        public string ComparePetUsage(string type, int lastRunPetTrait, int nextRunPetTrait)
        {
            string change = string.Empty;
            if (nextRunPetTrait != lastRunPetTrait)
            {
                // Pets used in last run - presume to reuse in next run
                if (lastRunPetTrait > 0)
                {
                    int diff = lastRunPetTrait - nextRunPetTrait;
                    if (diff > 0)
                    {
                        change = string.Concat("Applied ", type, " pet(s) used in Last run : ", diff > 0 ? "+" : "", diff);
                    }
                    else
                    {
                        change = string.Concat("New ", type, " Pet(s) applied since Last run : ", diff > 0 ? "+" : "", -diff);
                    }
                }
                else  // Current pets assigned not in use in last run.
                {
                    change = string.Concat("New ", type, " pet(s) applied since Last run : +", nextRunPetTrait);
                }
            }
            return change;
        }

        private int GetBuildingProduce(int buildingTypeId, int buildingId, int actionId)
        {
            // ETH WORLD ENH needed to support Steel,  uses same buildingId(9) but product is steel (need to have some check on current client system active)
            return buildingTypeId == (int)BUILDING_TYPE.INDUSTRIAL ? actionId :
                buildingId switch
                {
                    (int)BUILDING_SUBTYPE.BRICKWORKS => (int)BUILDING_PRODUCT.BRICK,
                    (int)BUILDING_SUBTYPE.GLASSWORKS => (int)BUILDING_PRODUCT.GLASS,
                    (int)BUILDING_SUBTYPE.CONCRETE_PLANT => (int)BUILDING_PRODUCT.CONCRETE,
                    (int)BUILDING_SUBTYPE.POWER_PLANT => (int)BUILDING_PRODUCT.ENERGY,
                    (int)BUILDING_SUBTYPE.WATER_PLANT => (int)BUILDING_PRODUCT.WATER,

                    _ => (int)BUILDING_PRODUCT.BRICK
                };
        }
        private IEnumerable<ResourceTotal> GetResourceTotalDisplay(List<ResourceTotal> resourceTotal, int multiplier, int buildingLevel)
        {
            List<ResourceTotal> formatedTotal = new();

            resourceTotal = resourceTotal.OrderBy(x => x.resourceId).ToList();

            for (int count = 0; count < resourceTotal.Count; count++)
            {
                formatedTotal.Add(new ResourceTotal()
                {
                    name = resourceTotal[count].name,
                    totalFormat = String.Format("{0:n0}", resourceTotal[count].total * multiplier * (7 * buildingLevel / 7))
                });
            }

            return formatedTotal;
        }

        private int CheckBuildingCitizenHistory(List<OwnerCitizenExt> citizens, DateTime eventDate, int buildingTokenId, string ownerMatic, int waitPeriodMS = 100)
        {
            CitizenManage citizenManage = new(_context);
            DBLogger dbLogger = new(_context);
            int actionCount = 0;

            // Citizens used by target run ( typically last production run, all OwnerCitizen records with valid_to_date after run date)
            List<OwnerCitizenExt> filterCitizens = citizens.Where(x => (x.valid_to_date >= eventDate || x.valid_to_date is null) && x.link_date < eventDate).ToList();

            for (int index = 0; index < filterCitizens.Count; index++)
            {
                //if (filterCitizens[index].token_id == 10257)
                //{
                //    var a = 1;
                //}

                //filterCitizens[index]
                actionCount += citizenManage.CitizenUpdateEvents(filterCitizens[index].link_key, eventDate, ownerMatic);
                WaitPeriodAction(waitPeriodMS).Wait();                //Wait set period required reduce load on MCP services - min 100ms
            }

            if (actionCount > 0)
            {
                _context.SaveChanges();
                dbLogger.logInfo(String.Concat(actionCount, " x Citizen history action records evaluated for account: ", ownerMatic, " and building:", buildingTokenId));
            }

            return actionCount;
        }

        private IEnumerable<string> AddChangeMessage(string message, IEnumerable<string> msgCollection)
        {
            if (msgCollection == null)
            {
                msgCollection = new string[] { message };
            }
            else
            {
                List<string> tempList = msgCollection.ToList();
                tempList.Add(message);
                msgCollection = tempList.ToArray();
            }

            return msgCollection;
        }

        // Citizen Efficiency calc per building uses following rules
        // Calculation evaluated on sum all citizens triats, not line by line per cit, impacts rounding.
        // Only one rounding rule applied - rounded to 2 places as final rule - after all other calcs - rounding on efficiency % (not produce here).
        // Calucate efficiency % as partial related to building type (eg 60% or 45%) and not 100% as shown in Citizen modules for easy eval - to get x/100% convert the partial.
        private decimal CalculateEfficiency_Citizen(List<OwnerCitizenExt> citizens, DateTime? eventDate, int? buildingLevel, int buildingType, int buildingProduct, PetUsage petUsageCompare)
        {
            decimal efficiency = 0;
            PetUsage petUsageOnEventDate = citizenManage.GetPetUsage(eventDate, citizens);

            // Identify any Pet usage difference between last run and current cits assigned to building.
            // Note that if a Pet is already assigned to a cit, then the cit traits will reflect it - dont double the Pet bonus.
            PetUsage petUsageDifference = new()
            {
                agility = petUsageCompare != null && petUsageCompare.agility > petUsageOnEventDate.agility ? petUsageCompare.agility - petUsageOnEventDate.agility : petUsageOnEventDate.agility,
                charisma = petUsageCompare != null && petUsageCompare.charisma > petUsageOnEventDate.charisma ? petUsageCompare.charisma - petUsageOnEventDate.charisma : petUsageOnEventDate.charisma,
                endurance = petUsageCompare != null && petUsageCompare.endurance > petUsageOnEventDate.endurance ? petUsageCompare.endurance - petUsageOnEventDate.endurance : petUsageOnEventDate.endurance,
                intelligence = petUsageCompare != null && petUsageCompare.intelligence > petUsageOnEventDate.intelligence ? petUsageCompare.intelligence - petUsageOnEventDate.intelligence : petUsageOnEventDate.intelligence,
                luck = petUsageCompare != null && petUsageCompare.luck > petUsageOnEventDate.luck ? petUsageCompare.luck - petUsageOnEventDate.luck : petUsageOnEventDate.luck,
                strength = petUsageCompare != null && petUsageCompare.strength > petUsageOnEventDate.strength ? petUsageCompare.strength - petUsageOnEventDate.strength : petUsageOnEventDate.strength
            };

            // Citizens used by run will be assigned min of 24hrs before run occured = link_date < eventDate
            List<OwnerCitizenExt> filterCitizens = citizens.Where(x => (x.valid_to_date >= eventDate || x.valid_to_date is null) && x.link_date < eventDate).ToList();

            int citizenMax = (buildingLevel ?? 0) switch
            {
                1 => 2,
                2 => 4,
                3 => 6,
                4 => 8,
                5 => 10,
                6 => 14,
                7 => 20,
                _ => 2
            };

            // rounding using MidpointRounding.AwayFromZero  results in 2.5 = 3,  without it using bankers rounding 2.5 = 2
            if (buildingType == (int)BUILDING_TYPE.INDUSTRIAL)
            {

                efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_strength) + petUsageDifference.strength) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_endurance) + petUsageDifference.endurance) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_agility + x.trait_charisma + x.trait_intelligence + x.trait_luck) + petUsageDifference.agility + petUsageDifference.charisma + petUsageDifference.intelligence) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }
            else if (buildingType == (int)BUILDING_TYPE.PRODUCTION)
            {
                efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength) + petUsageDifference.strength) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_endurance + x.trait_charisma + x.trait_intelligence + x.trait_luck) + petUsageDifference.endurance + petUsageDifference.charisma + petUsageDifference.intelligence) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }
            else if (buildingType == (int)BUILDING_TYPE.ENERGY)
            {
                if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                {
                    efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_endurance) + petUsageDifference.endurance) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength + x.trait_charisma + x.trait_intelligence + x.trait_luck) + petUsageDifference.strength + petUsageDifference.charisma + petUsageDifference.intelligence) / 4.0 * .1 / citizenMax
                    , 3) * 10;
                }
                else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                {
                    efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_endurance) + petUsageDifference.endurance) * .25 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .15 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength + x.trait_charisma + x.trait_intelligence + x.trait_luck) + petUsageDifference.strength + petUsageDifference.charisma + petUsageDifference.intelligence) / 4.0 * .05 / citizenMax
                    , 3) * 10;
                }
            }
            return efficiency;
        }

        private int CalculateEfficiency_Production(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
        {
            double efficiency = 0;

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.ENERGY:
                    if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_WATER.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_ENERGY.LEVEL_7,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.COMMERCIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.INDUSTRIAL:
                    if (buildingProduct == (int)BUILDING_PRODUCT.METAL)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_METAL.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.WOOD)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_WOOD.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.SAND)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_SAND.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STONE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_STONE.LEVEL_7,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.OFFICE:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.PRODUCTION:
                    if (buildingProduct == (int)BUILDING_PRODUCT.BRICK)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_BRICK.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.GLASS)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_GLASS.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_CONCRETE.LEVEL_7,
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }
            return (int)Math.Round(efficiency);
        }

        private int GetBaseProduce(int buildingType, int buildingLvl, int buildingProduct)
        {
            return buildingType switch
            {
                (int)BUILDING_TYPE.RESIDENTIAL => 0,

                (int)BUILDING_TYPE.ENERGY => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.WATER => buildingLvl switch
                    {
                        1 => (int)MIN_WATER.LEVEL_1,
                        2 => (int)MIN_WATER.LEVEL_2,
                        3 => (int)MIN_WATER.LEVEL_3,
                        4 => (int)MIN_WATER.LEVEL_4,
                        5 => (int)MIN_WATER.LEVEL_5,
                        6 => (int)MIN_WATER.LEVEL_6,
                        7 => (int)MIN_WATER.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.ENERGY => buildingLvl switch
                    {
                        1 => (int)MIN_ENERGY.LEVEL_1,
                        2 => (int)MIN_ENERGY.LEVEL_2,
                        3 => (int)MIN_ENERGY.LEVEL_3,
                        4 => (int)MIN_ENERGY.LEVEL_4,
                        5 => (int)MIN_ENERGY.LEVEL_5,
                        6 => (int)MIN_ENERGY.LEVEL_6,
                        7 => (int)MIN_ENERGY.LEVEL_7,
                        _ => 0
                    },
                    _ => 0
                },

                (int)BUILDING_TYPE.COMMERCIAL => 0,
                (int)BUILDING_TYPE.OFFICE => 0,

                (int)BUILDING_TYPE.INDUSTRIAL => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.METAL => buildingLvl switch
                    {
                        1 => (int)MIN_METAL.LEVEL_1,
                        2 => (int)MIN_METAL.LEVEL_2,
                        3 => (int)MIN_METAL.LEVEL_3,
                        4 => (int)MIN_METAL.LEVEL_4,
                        5 => (int)MIN_METAL.LEVEL_5,
                        6 => (int)MIN_METAL.LEVEL_6,
                        7 => (int)MIN_METAL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.WOOD => buildingLvl switch
                    {
                        1 => (int)MIN_WOOD.LEVEL_1,
                        2 => (int)MIN_WOOD.LEVEL_2,
                        3 => (int)MIN_WOOD.LEVEL_3,
                        4 => (int)MIN_WOOD.LEVEL_4,
                        5 => (int)MIN_WOOD.LEVEL_5,
                        6 => (int)MIN_WOOD.LEVEL_6,
                        7 => (int)MIN_WOOD.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.SAND => buildingLvl switch
                    {
                        1 => (int)MIN_SAND.LEVEL_1,
                        2 => (int)MIN_SAND.LEVEL_2,
                        3 => (int)MIN_SAND.LEVEL_3,
                        4 => (int)MIN_SAND.LEVEL_4,
                        5 => (int)MIN_SAND.LEVEL_5,
                        6 => (int)MIN_SAND.LEVEL_6,
                        7 => (int)MIN_SAND.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.STONE => buildingLvl switch
                    {
                        1 => (int)MIN_STONE.LEVEL_1,
                        2 => (int)MIN_STONE.LEVEL_2,
                        3 => (int)MIN_STONE.LEVEL_3,
                        4 => (int)MIN_STONE.LEVEL_4,
                        5 => (int)MIN_STONE.LEVEL_5,
                        6 => (int)MIN_STONE.LEVEL_6,
                        7 => (int)MIN_STONE.LEVEL_7,
                        _ => 0
                    },

                    _ => 0
                },

                (int)BUILDING_TYPE.PRODUCTION => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.BRICK => buildingLvl switch
                    {
                        1 => (int)MIN_BRICK.LEVEL_1,
                        2 => (int)MIN_BRICK.LEVEL_2,
                        3 => (int)MIN_BRICK.LEVEL_3,
                        4 => (int)MIN_BRICK.LEVEL_4,
                        5 => (int)MIN_BRICK.LEVEL_5,
                        6 => (int)MIN_BRICK.LEVEL_6,
                        7 => (int)MIN_BRICK.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.GLASS => buildingLvl switch
                    {
                        1 => (int)MIN_GLASS.LEVEL_1,
                        2 => (int)MIN_GLASS.LEVEL_2,
                        3 => (int)MIN_GLASS.LEVEL_3,
                        4 => (int)MIN_GLASS.LEVEL_4,
                        5 => (int)MIN_GLASS.LEVEL_5,
                        6 => (int)MIN_GLASS.LEVEL_6,
                        7 => (int)MIN_GLASS.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.STEEL => buildingLvl switch
                    {
                        1 => (int)MIN_STEEL.LEVEL_1,
                        2 => (int)MIN_STEEL.LEVEL_2,
                        3 => (int)MIN_STEEL.LEVEL_3,
                        4 => (int)MIN_STEEL.LEVEL_4,
                        5 => (int)MIN_STEEL.LEVEL_5,
                        6 => (int)MIN_STEEL.LEVEL_6,
                        7 => (int)MIN_STEEL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.CONCRETE => buildingLvl switch
                    {
                        1 => (int)MIN_CONCRETE.LEVEL_1,
                        2 => (int)MIN_CONCRETE.LEVEL_2,
                        3 => (int)MIN_CONCRETE.LEVEL_3,
                        4 => (int)MIN_CONCRETE.LEVEL_4,
                        5 => (int)MIN_CONCRETE.LEVEL_5,
                        6 => (int)MIN_CONCRETE.LEVEL_6,
                        7 => (int)MIN_CONCRETE.LEVEL_7,
                        _ => 0
                    },

                    _ => 0
                },

                _ => 0
            };

        }

        private int GetMaxProduce(int buildingType, int buildingLvl, int buildingProduct)
        {
            return buildingType switch
            {
                (int)BUILDING_TYPE.RESIDENTIAL => 0,

                (int)BUILDING_TYPE.ENERGY => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.WATER => buildingLvl switch
                    {
                        1 => (int)MAX_WATER.LEVEL_1,
                        2 => (int)MAX_WATER.LEVEL_2,
                        3 => (int)MAX_WATER.LEVEL_3,
                        4 => (int)MAX_WATER.LEVEL_4,
                        5 => (int)MAX_WATER.LEVEL_5,
                        6 => (int)MAX_WATER.LEVEL_6,
                        7 => (int)MAX_WATER.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.ENERGY => buildingLvl switch
                    {
                        1 => (int)MAX_ENERGY.LEVEL_1,
                        2 => (int)MAX_ENERGY.LEVEL_2,
                        3 => (int)MAX_ENERGY.LEVEL_3,
                        4 => (int)MAX_ENERGY.LEVEL_4,
                        5 => (int)MAX_ENERGY.LEVEL_5,
                        6 => (int)MAX_ENERGY.LEVEL_6,
                        7 => (int)MAX_ENERGY.LEVEL_7,
                        _ => 0
                    },
                    _ => 0
                },

                (int)BUILDING_TYPE.COMMERCIAL => 0,
                (int)BUILDING_TYPE.OFFICE => 0,

                (int)BUILDING_TYPE.INDUSTRIAL => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.METAL => buildingLvl switch
                    {
                        1 => (int)MAX_METAL.LEVEL_1,
                        2 => (int)MAX_METAL.LEVEL_2,
                        3 => (int)MAX_METAL.LEVEL_3,
                        4 => (int)MAX_METAL.LEVEL_4,
                        5 => (int)MAX_METAL.LEVEL_5,
                        6 => (int)MAX_METAL.LEVEL_6,
                        7 => (int)MAX_METAL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.WOOD => buildingLvl switch
                    {
                        1 => (int)MAX_WOOD.LEVEL_1,
                        2 => (int)MAX_WOOD.LEVEL_2,
                        3 => (int)MAX_WOOD.LEVEL_3,
                        4 => (int)MAX_WOOD.LEVEL_4,
                        5 => (int)MAX_WOOD.LEVEL_5,
                        6 => (int)MAX_WOOD.LEVEL_6,
                        7 => (int)MAX_WOOD.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.SAND => buildingLvl switch
                    {
                        1 => (int)MAX_SAND.LEVEL_1,
                        2 => (int)MAX_SAND.LEVEL_2,
                        3 => (int)MAX_SAND.LEVEL_3,
                        4 => (int)MAX_SAND.LEVEL_4,
                        5 => (int)MAX_SAND.LEVEL_5,
                        6 => (int)MAX_SAND.LEVEL_6,
                        7 => (int)MAX_SAND.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.STONE => buildingLvl switch
                    {
                        1 => (int)MAX_STONE.LEVEL_1,
                        2 => (int)MAX_STONE.LEVEL_2,
                        3 => (int)MAX_STONE.LEVEL_3,
                        4 => (int)MAX_STONE.LEVEL_4,
                        5 => (int)MAX_STONE.LEVEL_5,
                        6 => (int)MAX_STONE.LEVEL_6,
                        7 => (int)MAX_STONE.LEVEL_7,
                        _ => 0
                    },

                    _ => 0
                },

                (int)BUILDING_TYPE.PRODUCTION => buildingProduct switch
                {
                    (int)BUILDING_PRODUCT.BRICK => buildingLvl switch
                    {
                        1 => (int)MAX_BRICK.LEVEL_1,
                        2 => (int)MAX_BRICK.LEVEL_2,
                        3 => (int)MAX_BRICK.LEVEL_3,
                        4 => (int)MAX_BRICK.LEVEL_4,
                        5 => (int)MAX_BRICK.LEVEL_5,
                        6 => (int)MAX_BRICK.LEVEL_6,
                        7 => (int)MAX_BRICK.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.GLASS => buildingLvl switch
                    {
                        1 => (int)MAX_GLASS.LEVEL_1,
                        2 => (int)MAX_GLASS.LEVEL_2,
                        3 => (int)MAX_GLASS.LEVEL_3,
                        4 => (int)MAX_GLASS.LEVEL_4,
                        5 => (int)MAX_GLASS.LEVEL_5,
                        6 => (int)MAX_GLASS.LEVEL_6,
                        7 => (int)MAX_GLASS.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.STEEL => buildingLvl switch
                    {
                        1 => (int)MAX_STEEL.LEVEL_1,
                        2 => (int)MAX_STEEL.LEVEL_2,
                        3 => (int)MAX_STEEL.LEVEL_3,
                        4 => (int)MAX_STEEL.LEVEL_4,
                        5 => (int)MAX_STEEL.LEVEL_5,
                        6 => (int)MAX_STEEL.LEVEL_6,
                        7 => (int)MAX_STEEL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.CONCRETE => buildingLvl switch
                    {
                        1 => (int)MAX_CONCRETE.LEVEL_1,
                        2 => (int)MAX_CONCRETE.LEVEL_2,
                        3 => (int)MAX_CONCRETE.LEVEL_3,
                        4 => (int)MAX_CONCRETE.LEVEL_4,
                        5 => (int)MAX_CONCRETE.LEVEL_5,
                        6 => (int)MAX_CONCRETE.LEVEL_6,
                        7 => (int)MAX_CONCRETE.LEVEL_7,
                        _ => 0
                    },

                    _ => 0
                },

                _ => 0
            };

        }

        private int CalculateEfficiency_MinMax(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
        {
            double efficiency = 0.0;

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.ENERGY:
                    if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_WATER.LEVEL_1) * 100d / (750 - (int)MIN_WATER.LEVEL_1),
                            2 => (amount_produced - (int)MIN_WATER.LEVEL_2) * 100d / (836 - (int)MIN_WATER.LEVEL_2),
                            3 => (amount_produced - (int)MIN_WATER.LEVEL_3) * 100d / (938 - (int)MIN_WATER.LEVEL_3),
                            4 => (amount_produced - (int)MIN_WATER.LEVEL_4) * 100d / (1071 - (int)MIN_WATER.LEVEL_4),
                            5 => (amount_produced - (int)MIN_WATER.LEVEL_5) * 100d / (1286 - (int)MIN_WATER.LEVEL_5),
                            6 => (amount_produced - (int)MIN_WATER.LEVEL_6) * 100d / (2143 - (int)MIN_WATER.LEVEL_6),
                            7 => (amount_produced - (int)MIN_WATER.LEVEL_7) * 100d / (3321 - (int)MIN_WATER.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_ENERGY.LEVEL_1) * 100d / (5000 - (int)MIN_ENERGY.LEVEL_1),
                            2 => (amount_produced - (int)MIN_ENERGY.LEVEL_2) * 100d / (5571 - (int)MIN_ENERGY.LEVEL_2),
                            3 => (amount_produced - (int)MIN_ENERGY.LEVEL_3) * 100d / (6250 - (int)MIN_ENERGY.LEVEL_3),
                            4 => (amount_produced - (int)MIN_ENERGY.LEVEL_4) * 100d / (7143 - (int)MIN_ENERGY.LEVEL_4),
                            5 => (amount_produced - (int)MIN_ENERGY.LEVEL_5) * 100d / (8571 - (int)MIN_ENERGY.LEVEL_5),
                            6 => (amount_produced - (int)MIN_ENERGY.LEVEL_6) * 100d / (14286 - (int)MIN_ENERGY.LEVEL_6),
                            7 => (amount_produced - (int)MIN_ENERGY.LEVEL_7) * 100d / (22143 - (int)MIN_ENERGY.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.COMMERCIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.INDUSTRIAL:
                    if (buildingProduct == (int)BUILDING_PRODUCT.METAL)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_METAL.LEVEL_1) * 100d / (160 - (int)MIN_METAL.LEVEL_1),
                            2 => (amount_produced - (int)MIN_METAL.LEVEL_2) * 100d / (178 - (int)MIN_METAL.LEVEL_2),
                            3 => (amount_produced - (int)MIN_METAL.LEVEL_3) * 100d / (200 - (int)MIN_METAL.LEVEL_3),
                            4 => (amount_produced - (int)MIN_METAL.LEVEL_4) * 100d / (229 - (int)MIN_METAL.LEVEL_4),
                            5 => (amount_produced - (int)MIN_METAL.LEVEL_5) * 100d / (274 - (int)MIN_METAL.LEVEL_5),
                            6 => (amount_produced - (int)MIN_METAL.LEVEL_6) * 100d / (457 - (int)MIN_METAL.LEVEL_6),
                            7 => (amount_produced - (int)MIN_METAL.LEVEL_7) * 100d / (594 - (int)MIN_METAL.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.WOOD)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_WOOD.LEVEL_1) * 100d / (375 - (int)MIN_WOOD.LEVEL_1),
                            2 => (amount_produced - (int)MIN_WOOD.LEVEL_2) * 100d / (418 - (int)MIN_WOOD.LEVEL_2),
                            3 => (amount_produced - (int)MIN_WOOD.LEVEL_3) * 100d / (469 - (int)MIN_WOOD.LEVEL_3),
                            4 => (amount_produced - (int)MIN_WOOD.LEVEL_4) * 100d / (536 - (int)MIN_WOOD.LEVEL_4),
                            5 => (amount_produced - (int)MIN_WOOD.LEVEL_5) * 100d / (643 - (int)MIN_WOOD.LEVEL_5),
                            6 => (amount_produced - (int)MIN_WOOD.LEVEL_6) * 100d / (1071 - (int)MIN_WOOD.LEVEL_6),
                            7 => (amount_produced - (int)MIN_WOOD.LEVEL_7) * 100d / (1393 - (int)MIN_WOOD.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.SAND)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_SAND.LEVEL_1) * 100d / (225 - (int)MIN_SAND.LEVEL_1),
                            2 => (amount_produced - (int)MIN_SAND.LEVEL_2) * 100d / (251 - (int)MIN_SAND.LEVEL_2),
                            3 => (amount_produced - (int)MIN_SAND.LEVEL_3) * 100d / (281 - (int)MIN_SAND.LEVEL_3),
                            4 => (amount_produced - (int)MIN_SAND.LEVEL_4) * 100d / (321 - (int)MIN_SAND.LEVEL_4),
                            5 => (amount_produced - (int)MIN_SAND.LEVEL_5) * 100d / (386 - (int)MIN_SAND.LEVEL_5),
                            6 => (amount_produced - (int)MIN_SAND.LEVEL_6) * 100d / (643 - (int)MIN_SAND.LEVEL_6),
                            7 => (amount_produced - (int)MIN_SAND.LEVEL_7) * 100d / (836 - (int)MIN_SAND.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STONE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_STONE.LEVEL_1) * 100d / ((int)MAX_STONE.LEVEL_1 - (int)MIN_STONE.LEVEL_1),
                            2 => (amount_produced - (int)MIN_STONE.LEVEL_2) * 100d / ((int)MAX_STONE.LEVEL_2 - (int)MIN_STONE.LEVEL_2),
                            3 => (amount_produced - (int)MIN_STONE.LEVEL_3) * 100d / ((int)MAX_STONE.LEVEL_3 - (int)MIN_STONE.LEVEL_3),
                            4 => (amount_produced - (int)MIN_STONE.LEVEL_4) * 100d / ((int)MAX_STONE.LEVEL_4 - (int)MIN_STONE.LEVEL_4),
                            5 => (amount_produced - (int)MIN_STONE.LEVEL_5) * 100d / ((int)MAX_STONE.LEVEL_5 - (int)MIN_STONE.LEVEL_5),
                            6 => (amount_produced - (int)MIN_STONE.LEVEL_6) * 100d / ((int)MAX_STONE.LEVEL_6 - (int)MIN_STONE.LEVEL_6),
                            7 => (amount_produced - (int)MIN_STONE.LEVEL_7) * 100d / ((int)MAX_STONE.LEVEL_7 - (int)MIN_STONE.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.OFFICE:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.PRODUCTION:
                    if (buildingProduct == (int)BUILDING_PRODUCT.BRICK)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_BRICK.LEVEL_1) * 100d / (40 - (int)MIN_BRICK.LEVEL_1),
                            2 => (amount_produced - (int)MIN_BRICK.LEVEL_2) * 100d / (45 - (int)MIN_BRICK.LEVEL_2),
                            3 => (amount_produced - (int)MIN_BRICK.LEVEL_3) * 100d / (50 - (int)MIN_BRICK.LEVEL_3),
                            4 => (amount_produced - (int)MIN_BRICK.LEVEL_4) * 100d / (57 - (int)MIN_BRICK.LEVEL_4),
                            5 => (amount_produced - (int)MIN_BRICK.LEVEL_5) * 100d / (69 - (int)MIN_BRICK.LEVEL_5),
                            6 => (amount_produced - (int)MIN_BRICK.LEVEL_6) * 100d / (114 - (int)MIN_BRICK.LEVEL_6),
                            7 => (amount_produced - (int)MIN_BRICK.LEVEL_7) * 100d / (177 - (int)MIN_BRICK.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.GLASS)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_GLASS.LEVEL_1) * 100d / (32 - (int)MIN_GLASS.LEVEL_1),
                            2 => (amount_produced - (int)MIN_GLASS.LEVEL_2) * 100d / (36 - (int)MIN_GLASS.LEVEL_2),
                            3 => (amount_produced - (int)MIN_GLASS.LEVEL_3) * 100d / (40 - (int)MIN_GLASS.LEVEL_3),
                            4 => (amount_produced - (int)MIN_GLASS.LEVEL_4) * 100d / (46 - (int)MIN_GLASS.LEVEL_4),
                            5 => (amount_produced - (int)MIN_GLASS.LEVEL_5) * 100d / (55 - (int)MIN_GLASS.LEVEL_5),
                            6 => (amount_produced - (int)MIN_GLASS.LEVEL_6) * 100d / (91 - (int)MIN_GLASS.LEVEL_6),
                            7 => (amount_produced - (int)MIN_GLASS.LEVEL_7) * 100d / (142 - (int)MIN_GLASS.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_CONCRETE.LEVEL_1) * 100d / ((int)MAX_CONCRETE.LEVEL_1 - (int)MIN_CONCRETE.LEVEL_1),
                            2 => (amount_produced - (int)MIN_CONCRETE.LEVEL_2) * 100d / ((int)MAX_CONCRETE.LEVEL_2 - (int)MIN_CONCRETE.LEVEL_3),
                            3 => (amount_produced - (int)MIN_CONCRETE.LEVEL_3) * 100d / ((int)MAX_CONCRETE.LEVEL_3 - (int)MIN_CONCRETE.LEVEL_3),
                            4 => (amount_produced - (int)MIN_CONCRETE.LEVEL_4) * 100d / ((int)MAX_CONCRETE.LEVEL_4 - (int)MIN_CONCRETE.LEVEL_4),
                            5 => (amount_produced - (int)MIN_CONCRETE.LEVEL_5) * 100d / ((int)MAX_CONCRETE.LEVEL_5 - (int)MIN_CONCRETE.LEVEL_5),
                            6 => (amount_produced - (int)MIN_CONCRETE.LEVEL_6) * 100d / ((int)MAX_CONCRETE.LEVEL_6 - (int)MIN_CONCRETE.LEVEL_6),
                            7 => (amount_produced - (int)MIN_CONCRETE.LEVEL_7) * 100d / ((int)MAX_CONCRETE.LEVEL_7 - (int)MIN_CONCRETE.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }
            return (int)Math.Round(efficiency);
        }

        private string GetBuildingTypeDesc(int buildingType)
        {
            return buildingType switch
            {
                (int)BUILDING_TYPE.INDUSTRIAL => "Industry",
                (int)BUILDING_TYPE.RESIDENTIAL => "Residential",
                (int)BUILDING_TYPE.PRODUCTION => "Production",
                (int)BUILDING_TYPE.COMMERCIAL => "Commercial",
                (int)BUILDING_TYPE.MUNICIPAL => "Municipal",
                (int)BUILDING_TYPE.ENERGY => "Energy",
                (int)BUILDING_TYPE.OFFICE => "Office",
                _ => ""
            };
        }

        private string GetApplianceName(int buildingProduct)
        {
            return buildingProduct switch
            {
                (int) APPLICATION.RED_SAT => "Red Sat",    // Red Sat (7) = 10% bonus
                (int) APPLICATION.WHITE_SAT => "White Sat",     // White Sat
                (int) APPLICATION.GREEN_AIR_CON => "Green Air Con",     // Green Air con
                (int) APPLICATION.WHITE_AIR_CON => "White Air Con",     // White Air con
                (int) APPLICATION.CCTV_RED => "CCTV Red",     // CCTV RED 
                (int) APPLICATION.CCTV_WHITE => "CCTV White",     // CCTV White                            
                (int) APPLICATION.ROUTER_BLACK => "Router Black",     // Router Black 
                (int) APPLICATION.RED_FIRE_ALARM => "Red Fire Alarm",     // Red Fire Alarm
                _ => "Product"
            };
        }

        private string GetResourceName(int buildingProduct)
        {
            return buildingProduct switch
            {
                (int)BUILDING_PRODUCT.WOOD => "Wood",
                (int)BUILDING_PRODUCT.SAND => "Sand",
                (int)BUILDING_PRODUCT.METAL => "Metal",
                (int)BUILDING_PRODUCT.BRICK => "Brick",
                (int)BUILDING_PRODUCT.GLASS => "Glass",
                (int)BUILDING_PRODUCT.CONCRETE => "Concrete",
                (int)BUILDING_PRODUCT.STONE => "Stone",
                (int)BUILDING_PRODUCT.STEEL => "Steel",
                (int)BUILDING_PRODUCT.WATER => "Water",
                (int)BUILDING_PRODUCT.ENERGY => "Energy",
                (int)BUILDING_PRODUCT.FACTORY_PRODUCT => "Factory",
                _ => "Product"
            };
        }

        private int GetElectricResouceLevel(int level)
        {
            return level switch
            {
                1 => (int)ELECTRIC_RESOURCE.LEVEL_1,
                2 => (int)ELECTRIC_RESOURCE.LEVEL_2,
                3 => (int)ELECTRIC_RESOURCE.LEVEL_3,
                4 => (int)ELECTRIC_RESOURCE.LEVEL_4,
                5 => (int)ELECTRIC_RESOURCE.LEVEL_5,
                6 => (int)ELECTRIC_RESOURCE.LEVEL_6,
                7 => (int)ELECTRIC_RESOURCE.LEVEL_7,
                8 => (int)ELECTRIC_RESOURCE.LEVEL_8,
                9 => (int)ELECTRIC_RESOURCE.LEVEL_9,
                10 => (int)ELECTRIC_RESOURCE.LEVEL_10,                
                _ => 0
            };
        }

        private int GetWaterResouceLevel(int level)
        {
            return level switch
            {
                1 => (int)WATER_RESOURCE.LEVEL_1,
                2 => (int)WATER_RESOURCE.LEVEL_2,
                3 => (int)WATER_RESOURCE.LEVEL_3,
                4 => (int)WATER_RESOURCE.LEVEL_4,
                5 => (int)WATER_RESOURCE.LEVEL_5,
                6 => (int)WATER_RESOURCE.LEVEL_6,
                7 => (int)WATER_RESOURCE.LEVEL_7,
                8 => (int)WATER_RESOURCE.LEVEL_8,
                9 => (int)WATER_RESOURCE.LEVEL_9,
                10 => (int)WATER_RESOURCE.LEVEL_10,
                _ => 0
            };
        }

        private bool EvalBuildingHistory(DateTime? lastRunProduceDate, int buildingLevel)
        {
            bool result = false;

            if (lastRunProduceDate == null ||
               (buildingLevel == 1 && lastRunProduceDate < DateTime.Now.AddDays(-7)) ||
               (buildingLevel == 2 && lastRunProduceDate < DateTime.Now.AddDays(-6)) ||
               (buildingLevel == 3 && lastRunProduceDate < DateTime.Now.AddDays(-5)) ||
               (buildingLevel == 4 && lastRunProduceDate < DateTime.Now.AddDays(-4)) ||
               (buildingLevel == 5 && lastRunProduceDate < DateTime.Now.AddDays(-3)) ||
               (buildingLevel == 6 && lastRunProduceDate < DateTime.Now.AddDays(-2)) ||
               (buildingLevel == 7 && lastRunProduceDate < DateTime.Now.AddDays(-1)) )
            {
                result = true;
            }           

            return result;
        }

        // MCP rule: Min IP must be a value >0, impacts energy buildings, shown on newspaper building report.
        private decimal GetIPEfficiency(int totalIP, int rangeIP, int minIP)
        {
            decimal efficiency = 1;

            if (totalIP <= minIP)
            {
                efficiency = 0;
            }
            else if (rangeIP != 0)
            {
                efficiency = (totalIP - minIP) / (decimal)rangeIP;
            }
            if (efficiency >= 1)
            {
                var x = 2;
            }

            return efficiency;
        }

        // Find Standard Deviation from passed list,  deviation of -1 to 1 = 68.2% of all results.
        // https://www.statisticshowto.com/probability-and-statistics/standard-deviation/
        private double StandardDeviationGet(List<int> elements)
        {
            
            // A) Get (Sum)Squared / count
            var A = Math.Pow(elements.Sum(), 2) / elements.Count;

            var B = 0.0;
            for (int index=0; index<elements.Count; index++)
            {
                B += Math.Pow(elements[index], 2);

            }

            var C = B - A;

            //Get variance 
            var D = C / (elements.Count - 1);

            var E = Math.Sqrt(D);

            return E;
        }

        // Z-Score : z = (x – μ) / σ
        // How far from the mean(avg) a data point is, Or how many standard deviations below or above mean the score is.
        private double CalcZscore(int elementVal, double mean, double standardDeviation)
        {
            return (elementVal - mean) / standardDeviation;
        }
    }
}
