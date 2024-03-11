using MetaverseMax.Database;
using MetaverseMax.BaseClass;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.Text;
using System;
using Microsoft.VisualBasic;


namespace MetaverseMax.ServiceClass
{
    public class BuildingManage : ServiceBase
    {
        private CitizenManage citizenManage;
        private ServiceCommon common = new();
        //private List<OwnerCitizenExt> ownerCitizenExt = new();
        private BuildingCollection buildingCollection = new();
        private const int USE_STORED_EFFICIENCY = -1;
        //private BuildingHistory buildingHistory;

        public BuildingManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _context.worldTypeSelected = worldTypeSelected;
            citizenManage = new(_parentContext, worldType);
        }

        public RETURN_CODE UpdateIPRanking(int waitPeriodMS = 100)
        {
            for (int level = 1; level < 8; level++)
            {
                BuildingIPbyTypeGet((int)BUILDING_TYPE.RESIDENTIAL, level, true, waitPeriodMS, true, "SYSTEM");
                BuildingIPbyTypeGet((int)BUILDING_TYPE.INDUSTRIAL, level, true, waitPeriodMS, true, "SYSTEM");
                BuildingIPbyTypeGet((int)BUILDING_TYPE.PRODUCTION, level, true, waitPeriodMS, true, "SYSTEM");
                BuildingIPbyTypeGet((int)BUILDING_TYPE.ENERGY, level, true, waitPeriodMS, true, "SYSTEM");
                BuildingIPbyTypeGet((int)BUILDING_TYPE.COMMERCIAL, level, true, waitPeriodMS, true, "SYSTEM");
                BuildingIPbyTypeGet((int)BUILDING_TYPE.MUNICIPAL, level, true, waitPeriodMS, true, "SYSTEM");
            }

            return RETURN_CODE.SUCCESS;
        }

        public RETURN_CODE UpdateIPRankingByType(int type, int level, int waitPeriodMS, bool skipNoActiveCitizen, string requesterMatic)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            try
            {
                BuildingIPbyTypeGet(type, level, true, waitPeriodMS, skipNoActiveCitizen, requesterMatic);

                _context.SaveChanges();

                returnCode = RETURN_CODE.SUCCESS;
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BuildingManage.UpdateIPRankingByType() : Error on updating IP ranking for all buildings matching a specific building type and level"));
            }

            return returnCode;
        }


        // History Service call has a milisecond wait feature - to reduce overloading backend services (and risk block)
        // last_run_produce_predict : Bool set to true(1) if plot had a prior recent run (7 - 1 days depending on building lvl) +AND+ no major change to Plot since (IP, Cits, POI) then include plot in prediction eval total.
        //                            Set by  buildingHistory.use_prediction within GetHistory() >  plotDB.UpdatePlot().   Used with field plot.predict_produce and plot.last_run_produce
        public BuildingCollection BuildingIPbyTypeGet(int buildingType, int buildingLevel, bool evalHistory, int waitPeriodMS, bool skipNoActiveCitizen, string requesterMatic)
        {
            Building building = new();
            Dictionary<int, decimal> IPRank = new();
            List<BuildingTypeIP> buildingList = null;
            List<ResourceActiveWeb> activeBuildingList = new();
            int position = 1, correctAppBonus = 0, saveCounter = 0, buildingCanCollectCount = 0, buildingNoCitizenCount = 0;
            BuildingHistory buildingHistory;
            OwnerManage ownerManage = new(_context, worldType);
            List<Database.AlertTrigger> ownerAlerts = new(), allOwnerAlerts = new();
            AlertTriggerManager alertTrigger = new(_context, worldType);
            AlertManage alert = new(_context, worldType);

            try
            {
                BuildingTypeIPDB buildingTypeIPDB = new(_context);
                DistrictPerkDB districtPerkDB = new(_context);
                DistrictDB districtDB = new(_context);
                BuildingRanking buildingRanking = new();

                buildingCollection.show_prediction = ServiceCommon.showPrediction;

                List<District> districtList = districtList = districtDB.DistrictGetAll_Latest().ToList();
                List<DistrictPerk> districtPerkList = districtPerkDB.PerkGetAll_ByPerkType((int)DISTRICT_PERKS.EXTRA_SLOT_APPLIANCE_ALL_BUILDINGS);
                //MathNet.Numerics.Distributions.Normal distribution = new();

                buildingList = buildingTypeIPDB.BuildingTypeGet(buildingType, buildingLevel).ToList();

                if (buildingList.Count > 0)
                {
                    // CORRECT CODE ONCE MCP FIX IP BUG
                    //buildingCollection.maxIP = (int)Math.Round(buildingList.Max(x => x.influence_info * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);
                    //buildingCollection.minIP = buildingList.Where(x => x.influence_info <= 0).Count() > 0 ? 0 :
                    //    (int)Math.Round(buildingList.Where(x => x.influence_info > 0).Min(x => x.influence_info * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);


                    // TEMP CODE - UNTIL MCP FIX IP (influence_info is correct but MCP doesnt use it) BUG
                    buildingCollection.maxIP = buildingRanking.CalcMaxIP(buildingList);

                    // 2023_05_07 Min IP - Rule only check buildings that have a calcualted influence_info >=0
                    // Case: Energy buildings may have a negative IP, these buildings are not included in max - min IP check.
                    buildingCollection.minIP = buildingRanking.CalcMinIP(buildingList);                    

                    //buildingCollection.minIP = buildingList.Where(x => x.influence <= 0).Count() > 0 ? 0 :
                    //    (int)Math.Round(buildingList.Where(x => x.influence > 0).Min(x => x.influence * (1 + (x.influence_bonus / 100.0))), 0, MidpointRounding.AwayFromZero);

                    // Check if influence is negnative then set to 0 for use in average calc.
                    //buildingCollection.avgIP = Math.Round(buildingList.Average(x => (x.influence_info < 0 ? 0 : x.influence_info) * (1 + ((x.influence_bonus) / 100.0))), 0, MidpointRounding.AwayFromZero);
                    buildingCollection.avgIP = buildingRanking.CalcAvgIP(buildingList); 

                    buildingCollection.rangeIP = buildingCollection.maxIP - buildingCollection.minIP;

                    // Calc and Eval Produce Prediction : using building that produced in last 7 days.
                    //buildingPredictList = buildingList.Where(x => x.last_run_produce_date >= new DateTime(2022, 01, 30, 23, 00, 00)).ToList();
                    //buildingPredictionSummary(buildingList);

                    if (!requesterMatic.Contains("SYSTEM") && ownerManage.CheckOwnerExistsByMatic(requesterMatic))
                    {
                        ownerAlerts = alertTrigger.GetByType(requesterMatic, ALERT_TYPE.BUILDING_RANKING, 0);
                    }                   
                    // CHECK for any user alert active for this building
                    allOwnerAlerts = alertTrigger.GetByType("ALL", ALERT_TYPE.BUILDING_RANKING, 0);                    

                }

                buildingCollection.sync_date = common.LocalTimeFormatStandardFromUTC(string.Empty, _context.ActionTimeGet(ACTION_TYPE.PLOT));
                buildingCollection.building_type = GetBuildingTypeDesc(buildingType);
                buildingCollection.building_lvl = buildingLevel;
                buildingCollection.buildingIP_impact = (buildingType == (int)BUILDING_TYPE.RESIDENTIAL || buildingType == (int)BUILDING_TYPE.ENERGY) ? 20 : 40;
                buildingCollection.img_url = "https://play.mcp3d.com/assets/images/buildings/";
                List<ResourceTotal> resourceTotal = new();

                for (int i = 0; i < buildingList.Count; i++)
                {
                    decimal ipEfficiencyStored = 0;
                    buildingList[i].building_img = building.GetBuildingImg((BUILDING_TYPE)buildingType, buildingList[i].building_id, buildingLevel, worldType)
                        .Replace(buildingCollection.img_url, "");
                    
                    if (buildingList[i].influence_info != buildingList[i].influence && buildingList[i].influence_info >= 0)
                    {
                        // Visual bug caused by not including negative IP impact for POI-Monument active/deactive
                        buildingList[i].ip_warning = string.Concat("MCP [Base IP] bug : ", buildingList[i].influence_info, "[Correct] vs ", buildingList[i].influence, "[Incorrect shown in Plot History] ");
                    }                   

                    // CHECK if application perk is active - confirm IP is correct or bugged
                    DistrictPerk districtPerk = districtPerkList.Where(x => x.district_id == buildingList[i].district_id).FirstOrDefault();
                    if (districtPerk != null)
                    {
                        if (districtPerk.perk_level == 1)
                        {
                            correctAppBonus = buildingList[i].app_123_bonus + buildingList[i].app_4_bonus;
                        }
                        else
                        {
                            correctAppBonus = buildingList[i].app_123_bonus + buildingList[i].app_4_bonus + buildingList[i].app_5_bonus;
                        }

                        if (correctAppBonus != buildingList[i].influence_bonus)
                        {
                            buildingList[i].ip_warning = string.Concat("Application bug : using app bonus ", buildingList[i].influence_bonus, "% versus correct value of ", correctAppBonus, "%,", districtPerk.perk_level == 1 ? " ONLY" : "", " Perk Extra Slot ", districtPerk.perk_level, " is active");
                        }

                    }

                    //buildingList[i].total_ip = GetInfluenceTotal(buildingList[i].influence_info, buildingList[i].influence_bonus);    // USE THIS ONCE MCP FIX IP BUG
                    buildingList[i].total_ip = GetInfluenceTotal(buildingList[i].influence, buildingList[i].influence_bonus);

                    // Special Case: if only 1 building at this level assign full 100% IP eff, or if multiple buildings all at max
                    ipEfficiencyStored = buildingList[i].current_influence_rank;
                    if (buildingList.Count == 1 || buildingCollection.maxIP == buildingList[i].total_ip)
                    {
                        buildingList[i].current_influence_rank = 100;
                    }
                    else
                    {
                        buildingList[i].current_influence_rank = buildingRanking.GetIPEfficiency(buildingList[i].total_ip, buildingCollection.rangeIP, buildingCollection.minIP, _context);
                    }

                    buildingList[i].produce_tax = AssignTaxMatch(districtList.Where(x => x.district_id == buildingList[i].district_id).FirstOrDefault(), buildingType);

                    // Collate all recent produce - NOTE LIMITATION - any new building calcs wont show until next refresh - such as evals to identify recent collection.
                    UpdateResourceTotal(resourceTotal, buildingType, buildingList[i], buildingLevel, worldType);

                    // Check if Building able to execute a run collection - then eval building history. Pass IP efficiency.
                    // Limiters:
                    //   TO_DO  [2024/03] Only check buildings that HAVE been recently collected, many building may be READY to collect, but uncollected for extended periods of time. Without this check, those buildings would be proceeded EVERY NIGHTLY SYNC
                    // PURPOSE: Full update of building details - to identify if recent collection occured and amount collected, update next prediction calc
                    if (evalHistory && (buildingType == (int)BUILDING_TYPE.INDUSTRIAL || buildingType == (int)BUILDING_TYPE.PRODUCTION || buildingType == (int)BUILDING_TYPE.ENERGY)
                        && EvalBuildingHistory(buildingList[i].last_run_produce_date, buildingLevel) == true )
                    {
                        // Save each building's evaluated IP efficiency, latest run and predicted produce.
                        buildingHistory = GetHistory(buildingList[i].token_id, waitPeriodMS, false, false, buildingList[i].owner_matic, buildingList[i].current_influence_rank, string.Empty, skipNoActiveCitizen);

                        saveCounter++;
                        buildingCanCollectCount++;

                        if (buildingHistory == null)
                        {
                            buildingNoCitizenCount++;
                        }
                        else
                        {
                            WaitPeriodAction(waitPeriodMS).Wait();       // Wait set period required reduce load on MCP services - min 100ms
                        }

                        // Save every 30 History evals - improve performance on local db updates.
                        if (saveCounter >= 30 || i == buildingList.Count - 1)
                        {
                            _context.SaveWithRetry();
                            saveCounter = 0;
                        }
                    }
                    else
                    {
                        //Store all Building IP rank and Update DB in bulk
                        IPRank.Add(buildingList[i].token_id, buildingList[i].current_influence_rank);
                    }

                    // CHECK for any user alert active for this building - Ranking changed - then add new alert for those accounts.
                    if (ipEfficiencyStored != buildingList[i].current_influence_rank)
                    {
                        allOwnerAlerts.Where(x => x.id == buildingList[i].token_id).ToList().ForEach(x =>
                        {
                            alert.AddRankingAlert(x.matic_key, buildingList[i].owner_matic, buildingList[i].token_id, ipEfficiencyStored, buildingList[i].current_influence_rank, buildingLevel, building.BuildingType(buildingType, buildingList[i].building_id), buildingList[i].district_id, (ALERT_TYPE)x.key_type);
                        });
                    }
                }

                resourceTotal = resourceTotal.OrderBy(x => x.name).ToList();
                buildingCollection.total_produced = GetResourceTotalDisplay(resourceTotal, 1, buildingLevel);
                buildingCollection.total_produced_month = GetResourceTotalDisplay(resourceTotal, 4, buildingLevel);

                resourceTotal = resourceTotal.OrderBy(x => x.buildingSeq).ToList();
                for (int index = 0; index < resourceTotal.Count; index++)
                {
                    activeBuildingList.Add(new ResourceActiveWeb()
                    {
                        resource_id = resourceTotal[index].resourceId,
                        name = resourceTotal[index].name,
                        total = resourceTotal[index].buildingCount,
                        active = resourceTotal[index].buildingActive,
                        active_total_ip = resourceTotal[index].buildingActiveIP,
                        shutdown = resourceTotal[index].buildingCount - resourceTotal[index].buildingActive,
                        building_id = resourceTotal[index].buildingId,
                        building_img = resourceTotal[index].buildingImg,
                        building_name = resourceTotal[index].buildingName
                    });
                }
                buildingCollection.active_buildings = activeBuildingList.ToArray();

                // Calc and Eval Produce Prediction : using building that produced in last 7 days.
                // Refresh building list data if history eval step was completed as building prediction data may have changed/updated.
                // TO_DO >> Potential for PERF improvement here - Remove need to call expensive db sproc again
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

                // Update all buildings(residential, municipals, office type) with updated ranking, [these building did not complete a history check)
                if (IPRank.Count > 0)
                {
                    foreach (KeyValuePair<int, Decimal> plotRank in IPRank)
                    {
                        List<Plot> buildingPlotList = _context.plot.Where(x => x.token_id == plotRank.Key).ToList();
                        for (int plotIndex = 0; plotIndex < buildingPlotList.Count;  plotIndex++)
                        {
                            buildingPlotList[plotIndex].current_influence_rank = plotRank.Value;
                        }
                    }
                    _context.SaveChanges();
                }

                if (buildingCanCollectCount > 0)
                {
                    _context.LogEvent(String.Concat("Building type: " + GetBuildingTypeDesc(buildingType) + " Lvl: " + buildingLevel + " - Total : ", buildingList.Count, " with History Processed : ", buildingCanCollectCount - buildingNoCitizenCount));
                }

                if (buildingType == (int)BUILDING_TYPE.OFFICE)
                {
                    buildingCollection.office_summary = new();
                    buildingCollection.office_summary.active_total_ip = 1;
                    buildingCollection.office_summary.active_max_daily_distribution_per_ip = 1; 
                }

                buildingCollection.buildings = ConvertBuildingWeb(buildingList, ownerAlerts);
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BuildingManage.BuildingIPbyTypeGet() : Error on retrival of List matching buildingType: ", buildingType, "and level ", buildingLevel));
            }

            return buildingCollection;
        }

        private IEnumerable<BuildingIPWeb> ConvertBuildingWeb(List<BuildingTypeIP> buildingList, List<Database.AlertTrigger> ownerAlertList)
        {
            List<BuildingIPWeb> buildingIPWeb = new();
            OwnerManage ownerManage = new(_context, worldType);

            foreach (BuildingTypeIP building in buildingList)
            {
                OwnerAccount ownerAccount = ownerManage.GetOwnerAccountByMatic(building.owner_matic);
                string name = string.Empty;
                int avatarId = 0;

                if (ownerAccount != null) {
                    name = ownerAccount.name == string.Empty ? ownerAccount.discord_name ?? string.Empty : ownerAccount.name;
                    avatarId = ownerAccount.avatar_id;
                }

                buildingIPWeb.Add(new()
                {
                    id = building.token_id,
                    bid = building.building_id,                 // Supports filtering by building sub-type
                    dis = building.district_id,
                    pos_x = building.pos_x,
                    pos_y = building.pos_y,

                    pos = building.position,
                    rank = building.current_influence_rank,
                    ip_t = building.total_ip,                   // plot.influence_info * plot.influence_bonus
                    //ip_b = building.influence_info,           // TEMP CODE - USE THIS WHEN MCP FIX IP BUG
                    ip_i = GetInfluenceTotal(building.influence_info, building.influence_bonus),
                    ip_b = building.influence,
                    bon = building.influence_bonus,
                    name = name,
                    nid = avatarId,
                    name_m = building.owner_matic,
                    con = building.condition,
                    act = building.active_building ? 1 : 0,
                    acts = building.active_stamina ? 1 : 0,
                    oos = building.out_of_statina_alert ? 1 : 0,
                    pre = building.predict_eval_result ?? 0,
                    warn = building.ip_warning ?? "",
                    img = building.building_img,
                    price = building.current_price,
                    r_p = building.for_rent,                    // rent price
                    ren = building.rented == true ? 1 : 0,      // rented
                    poi = building.production_poi_bonus,
                    tax = building.produce_tax,
                    r = building.building_abundance,
                    al = ownerAlertList.Where( x=> x.id == building.token_id).Any() == true ? 1 : 0,
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

        // fullRefreshRequest = true : true flag only used by user triggered refresh event from ProductionHistory module : updates Plot with latest MCP WS
        public BuildingHistory GetHistory(int token_id, int waitPeriodMS, bool forceDBSave, bool fullRefreshRequest, string ownerMatic, decimal ipEfficiency, string requester, bool skipNoActiveCitizen)
        {
            BuildingHistory buildingHistory = new();
            try
            {
                int actionCount = 0;
                int storedIp = 0;
                List<HistoryProduction> historyProductionList = new();
                List<ResourceTotal> resourceTotal = new();
                OwnerCitizenExtDB ownerCitizenExtDB = new(_context);
                List<OwnerCitizenExt> ownerCitizenExt = null;
                PlotManage plotManage = new PlotManage(_context, worldType);
                PlotDB plotDB = new PlotDB(_context, worldType);
                List<Plot> buildingPlots = null;
                Plot targetPlot = null, mcpPlot = null;
                DateTime? targetPlotLastUpdated = null, lastRunTime = null;
                IEnumerable<string> storeChangesLastRunMsg;
                OwnerManage ownerManage = new(_context, worldType);
                OwnerAccount requestOwner = null;
                Boolean refreshAllowed = false;


                if (requester.IsNullOrEmpty())
                {
                    requestOwner = new();       // ProTools flag will be disabled
                }
                else
                {
                    requestOwner = ownerManage.FindOwnerByMatic(requester, string.Empty);

                    if (fullRefreshRequest == true && ownerManage.SetSlowDown(requester))
                    {
                        refreshAllowed = true;
                    }

                    buildingHistory.slowdown = ownerManage.GetSlowDown(requester);
                }

                buildingPlots = plotDB.GetPlotbyToken(token_id);
                if (buildingPlots.Count == 0)
                {
                    throw new Exception(string.Concat("No building found matching token : ", token_id));
                }
                else
                {
                    targetPlot = buildingPlots[0];
                }

                if (ipEfficiency == USE_STORED_EFFICIENCY)
                {
                    targetPlotLastUpdated = buildingPlots[0].last_updated;          // Used to prevent spaming of this feature - only allow refresh every x mins per building
                    ipEfficiency = targetPlot.current_influence_rank ?? 0;
                    ownerMatic = targetPlot.owner_matic;

                    // User initiated Refresh due to stale data - damage, ip, citizens, poi bonus, apps
                    if (refreshAllowed == true && targetPlotLastUpdated < DateTime.UtcNow.AddMinutes(-2))
                    {
                        // Check if IP has changed since last update (use first plot in building)
                        storedIp = GetInfluenceTotal(buildingPlots[0].influence_info ?? 0, buildingPlots[0].influence_bonus ?? 0);

                        // Update first plot - all related building plots update as well.
                        mcpPlot = plotManage.AddOrUpdatePlot(buildingPlots[0].plot_id, buildingPlots[0].pos_x, buildingPlots[0].pos_y, false);

                        _context.SaveChanges();

                        buildingPlots = plotDB.GetPlotbyToken(token_id);        // Refresh to get possibly updated IP / IP Bonus due to app change / POI change reactivated etc
                        targetPlot = buildingPlots[0];

                        if (storedIp != GetInfluenceTotal(buildingPlots[0].influence_info ?? 0, buildingPlots[0].influence_bonus ?? 0))
                        {
                            buildingHistory.changes_last_run = AddChangeMessage("REFRESH PAGE - IP change and Ranking Change Identified, all buildings need to be reevaluated", buildingHistory.changes_last_run);
                        }
                    }                    
                }
                // 2023/07 : CHECK if INDUSTRY type buildings, and action_id has changed from last run - then run Full Update to confirm product in current cycle.
                // This check will only occur on NIGHTLY SYNC jobs - passing in a ranking_ip_efficiency% && if building is tagged as recent production completed && last completed run product differs from current stored action_id 
                // GOAL : update plot.action_id, to better reflect current building production product, this call should also impact a few additional plots a night, which owner frequently changes the product type within Industry buildings.
                else if (targetPlot.building_type_id == (int)BUILDING_TYPE.INDUSTRIAL && targetPlot.action_id != targetPlot.last_run_produce_id)
                {
                    // Update first plot - all related building plots update as well.
                    mcpPlot = plotManage.AddOrUpdatePlot(buildingPlots[0].plot_id, buildingPlots[0].pos_x, buildingPlots[0].pos_y, false);

                    _context.SaveChanges();

                    buildingPlots = plotDB.GetPlotbyToken(token_id);        // Refresh to get possibly updated IP / IP Bonus due to app change / POI change reactivated etc
                    targetPlot = buildingPlots[0];
                }



                buildingHistory.owner_matic = ownerMatic;

                // Nighly Sync: skip processing building History with 0 citizens assigned - prediction eval is not applicable.
                ownerCitizenExtDB.GetBuildingCitizen(token_id, ref ownerCitizenExt);
                if (skipNoActiveCitizen == true && ownerCitizenExt.Where(x => x.valid_to_date is null).Count() == 0)
                {
                    plotDB.UpdatePlotRank(
                        token_id,
                        ipEfficiency,
                        false,
                        forceDBSave);

                    buildingHistory = null;
                }
                // OFFICE - History is processed differently then industry/production/energy
                else if (targetPlot.building_type_id == (int)BUILDING_TYPE.OFFICE || targetPlot.building_type_id == (int)BUILDING_TYPE.COMMERCIAL)
                {
                    // Get dates since citizens assigned and building_type = office
                    historyProductionList = GetOfficeHistory(targetPlot, ownerCitizenExt, (BUILDING_TYPE)targetPlot.building_type_id);
                    buildingHistory.detail = historyProductionList.OrderByDescending(x => x.run_datetimeDT).ToArray();

                }
                // ALL OTHER BUILDING TYPES - Industry, Production, Energy
                else
                {
                    TokenHistory tokenHistory = new(_context, worldType);
                    JArray productionHistory = Task.Run(() => tokenHistory.GetMCP(token_id, TOKEN_TYPE.PLOT)).Result;

                    if (productionHistory != null && productionHistory.Count > 0)
                    {

                        historyProductionList = EvalProductionHistory(buildingHistory, token_id, productionHistory, resourceTotal, false, ownerCitizenExt);

                        // If no production history for this building and owner, then only apply prediction.
                        if (historyProductionList.Count == 0)
                        {
                            // REFRESH - CHECK citizen actions from now back to last production run, if citizen from last run has been removed (identified in history) then get all account cits again and reeval
                            if (refreshAllowed == true && targetPlot != null && targetPlotLastUpdated < DateTime.UtcNow.AddMinutes(-2) & mcpPlot != null)
                            {
                                FullRecheckCitizens(buildingHistory, token_id, waitPeriodMS, targetPlot, null, mcpPlot.citizen, ref ownerCitizenExt);
                            }

                            // Main Prediction eval call
                            GetPrediction(buildingHistory, null, token_id, ipEfficiency, lastRunTime, ownerCitizenExt);

                            // Default clear any prior production run data for this building and set efficiency fields, may occur due to transfer/sale. no need to force a save.
                            plotDB.UpdatePlot(
                                token_id,
                                ipEfficiency,
                                0,
                                0,
                                0,
                                null,
                                false,
                                -1,
                                forceDBSave);

                        }
                        else if (historyProductionList.Count > 0)
                        {
                            lastRunTime = new DateTime(historyProductionList.First().run_datetimeDT);
                            buildingHistory.run_count = historyProductionList.Count;
                            buildingHistory.start_production = historyProductionList.Last().run_datetime;
                            buildingHistory.totalProduced = GetResourceTotalDisplay(resourceTotal, 1, 1);

                            // Calculate IP efficiency and prediction if data sent within method request
                            // Note
                            //      Use building level as recorded in last sync, not last run - as building upgrade may have occured.
                            //      historyProductionList[0] contains buidlings currently assigned citizens - for the current IN-PROGRESS run, used for eval of current run.

                            // REFRESH - CHECK citizen actions from now back to last production run, if citizen from last run has been removed (identified in history) then get all account cits again and reeval
                            if (refreshAllowed == true && targetPlot != null && targetPlotLastUpdated < DateTime.UtcNow.AddMinutes(-2) & mcpPlot != null)
                            {
                                historyProductionList[0] = FullRecheckCitizens(buildingHistory, token_id, waitPeriodMS, targetPlot, historyProductionList[0], mcpPlot.citizen, ref ownerCitizenExt);
                            }

                            // As Prediction may be reevaluated need to store any prior warnings and readd them (otherwise lost in rerun).
                            storeChangesLastRunMsg = buildingHistory.changes_last_run;

                            // Main Prediction eval call
                            GetPrediction(buildingHistory, historyProductionList[0], token_id, ipEfficiency, lastRunTime, ownerCitizenExt);

                            // RE-EVAL CASES : CHECK if citizen history needs to be evaluated for add/removal of pets, then get the pet change events and redo GetPrediction calc
                            if (buildingHistory.check_citizens == true)
                            {
                                // Find any missing Citizen events since last production run and save to db.                                
                                actionCount = CheckBuildingCitizenHistory(ownerCitizenExt, (DateTime)lastRunTime, token_id, buildingHistory.owner_matic, waitPeriodMS);

                                // PERF - if no changes found then dont continue re-eval
                                if (actionCount > 0)
                                {
                                    // Refresh Citizen list for this building due to prior step actions
                                    ownerCitizenExtDB.GetBuildingCitizen(token_id, ref ownerCitizenExt);

                                    // Reeval Prediction with updated OwnerCitizen's,  need to also reeval last production run - pulling in any missing pet usage added in last step.
                                    // Recalculate Citizen Eff% with pets on the last run.
                                    historyProductionList[0] = PopulateProductionDetails(historyProductionList[0], ownerCitizenExt);

                                    GetPrediction(buildingHistory, historyProductionList[0], token_id, ipEfficiency, lastRunTime, ownerCitizenExt);
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
                                token_id,
                                ipEfficiency,
                                buildingHistory.prediction.total,
                                historyProductionList[0].amount_produced,
                                buildingHistory.current_building_id == (int)BUILDING_SUBTYPE.FACTORY ? (int)BUILDING_PRODUCT.FACTORY_PRODUCT : historyProductionList[0].building_product_id,
                                lastRunTime,
                                buildingHistory.use_prediction,
                                buildingHistory.prediction.resource_lvl,
                                forceDBSave);

                        }


                        // Add final History table row - comprising of current building Cit and next run prediction details, set as first row in returned list.
                        if (buildingHistory.prediction != null)
                        {
                            HistoryProduction pendingProductionRun = new()
                            {
                                seq = 1,
                                building_product = buildingHistory.prediction_product,
                                building_type = buildingHistory.current_building_type,
                                building_lvl = buildingHistory.current_building_lvl,
                                amount_produced = buildingHistory.prediction.total,
                                building_product_id = buildingHistory.prediction.product_id,
                                run_datetime = "In-Progress Prediction",
                                building_ip = GetInfluenceTotal(buildingPlots[0].influence ?? 0, buildingPlots[0].influence_bonus ?? 0),           // Replace with infuence_info when MCP fix influance issue
                                run_datetimeDT = DateTime.UtcNow.Ticks,
                                efficiency_c = buildingHistory.prediction.cit_efficiency,
                                efficiency_c_60 = buildingHistory.prediction.cit_efficiency_rounded,
                                efficiency_m = buildingHistory.prediction.efficiency_m,
                                efficiency_p = buildingHistory.prediction.efficiency_p,
                            };

                            // Only add current production run row if citizens are currently assigned to building - which is indicator of active run - note may been active for a long time with pending collection.
                            if (pendingProductionRun.efficiency_c > 0 && requestOwner.pro_tools_enabled)
                            {
                                historyProductionList.Add(pendingProductionRun);
                            }
                        }
                        buildingHistory.detail = historyProductionList.OrderByDescending(x => x.run_datetimeDT).ToArray();

                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BuildingManage.GetHistory() : Error occur with asset id : ", token_id));
            }

            return buildingHistory;
        }

        public List<HistoryProduction> GetOfficeHistory(Plot targetPlot, List<OwnerCitizenExt> ownerCitizenList, BUILDING_TYPE buildingType)
        {
            List<HistoryProduction> historyProductionList = new();
            DateTime? startDate = ownerCitizenList.Count > 0 ? ownerCitizenList.Min(x => x.create_date) : null;
            DateTime? evalDay = null;
            PlotDB plotDB = new PlotDB(_context, worldType);
            List<PlotIP> plotIPList = plotDB.GetIP_Historic(targetPlot.token_id);
            PlotIP plotIPData;
            decimal priorDayCitizenEff = -1;
            HistoryProduction processDay;

            // Filter only office IP , will only show Citizen history records for office building type (plot may have been a different building type).
            plotIPList = plotIPList.Where(x => x.building_type_id == (int)buildingType).ToList();

            // Add today
            HistoryProduction currentDay = new();
            currentDay.building_lvl = targetPlot.building_level;
            currentDay.building_ip = GetInfluenceTotal(targetPlot.influence ?? 0, targetPlot.influence_bonus ?? 0);           // Replace with infuence_info when MCP fix influance issue
            currentDay.efficiency_c_60 = CalculateEfficiency_Citizen(
                    ownerCitizenList,
                    DateTime.UtcNow,
                    targetPlot.building_level,
                    targetPlot.building_type_id,
                    BUILDING_PRODUCT.MEGA_PRODUCT_GLOBAL,
                    null);
            currentDay.efficiency_c = Math.Round(currentDay.efficiency_c_60 / .6m, 1, MidpointRounding.AwayFromZero);
            currentDay.run_datetime = common.DateStandard(DateTime.Now);
            currentDay.run_datetimeDT = DateTime.Now.Ticks;

            historyProductionList.Add(currentDay);

            // set time to midnight - collection time.
            if (startDate != null) {
                startDate = ((DateTime)startDate).AddDays(1);
                startDate = ((DateTime)startDate).Date;
            }

            evalDay = startDate;
            while (evalDay != null && evalDay < DateTime.UtcNow)
            {
                plotIPData = plotIPList.Where(x => x.last_updated <= (DateTime)evalDay).OrderByDescending(x => x.last_updated).FirstOrDefault();

                if (plotIPData != null)
                {
                    processDay = new();
                    processDay.building_lvl = plotIPData.building_level;
                    processDay.building_type = plotIPData.building_type_id;
                    processDay.building_ip = GetInfluenceTotal(plotIPData.influence ?? 0, plotIPData.influence_bonus ?? 0);           // Replace with infuence_info when MCP fix influance issue
                    processDay.efficiency_c_60 = CalculateEfficiency_Citizen(
                            ownerCitizenList,
                            evalDay,
                            processDay.building_lvl,
                            processDay.building_type,
                            BUILDING_PRODUCT.MEGA_PRODUCT_GLOBAL,
                            null);
                    processDay.efficiency_c = Math.Round(processDay.efficiency_c_60 / .6m, 1, MidpointRounding.AwayFromZero);
                    processDay.run_datetime = common.DateStandard(evalDay);
                    processDay.run_datetimeDT = ((DateTime)evalDay).Ticks;

                    if (priorDayCitizenEff != 0 || (priorDayCitizenEff == 0 && processDay.efficiency_c_60 != 0))
                    {
                        historyProductionList.Add(processDay);
                    }

                    priorDayCitizenEff = processDay.efficiency_c_60;
                }

                evalDay = (evalDay.Value.AddDays(1));                
            }

            return historyProductionList;
        }

        public OfficeGlobal OfficeGlobalSummaryGet()
        {
            OfficeGlobal officeGlobal = new();
            PlotDB plotDB = new(_context);
            DistrictFund globalFund;            
            ServiceCommon common = new ServiceCommon();

            try
            {
                officeGlobal.totalIP = plotDB.GetGlobalOfficeIP();
                globalFund = _context.districtFund.Where(x => x.district_id == 0).OrderByDescending(x => x.fund_key).FirstOrDefault();

                if (globalFund != null) {
                    officeGlobal.globalFund = (long)Math.Round(globalFund.balance, 0, MidpointRounding.AwayFromZero);
                    officeGlobal.maxDailyDistribution = Math.Round(officeGlobal.globalFund / 365, 0, MidpointRounding.AwayFromZero);
                    officeGlobal.maxDailyDistributionPerIP = Math.Round(officeGlobal.maxDailyDistribution / (officeGlobal.totalIP / 1000), 4, MidpointRounding.AwayFromZero);
                    officeGlobal.lastDistribution = (long)Math.Round(globalFund.distribution, 0, MidpointRounding.AwayFromZero);
                    officeGlobal.lastDistributionDate = common.DateFormatStandard(globalFund.last_updated);
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BuildingManage.OfficeGlobalSummaryGet() : Error on WS calls for balances"));
            }

            return officeGlobal;
        }
        public async Task<OfficeGlobal> OfficeGlobalSummaryGetLegacy()
        {
            OfficeGlobal officeGlobal = new();
            PlotDB plotDB = new(_context);
            HttpResponseMessage response;
            string content = string.Empty;
            decimal globalFund = 0;
            serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.BALANCES, WORLD_TYPE.BNB => BNB_WS.BALANCES, WORLD_TYPE.ETH => ETH_WS.BALANCES, _ => TRON_WS.BALANCES};

            try
            {
                officeGlobal.totalIP = plotDB.GetGlobalOfficeIP();

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
                servicePerfDB.AddServiceEntry("Building - " + serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Empty);

                JObject jsonContent = JObject.Parse(content);
                JToken banks = jsonContent.Value<JToken>("banks");
                if (banks != null && banks.HasValues)
                {
                    globalFund = banks.Value<Decimal?>("global") ?? 0;
                    officeGlobal.globalFund = (long)Math.Round(globalFund / 1000000000000000000, 0, MidpointRounding.AwayFromZero); // 18 places back
                    officeGlobal.maxDailyDistribution = Math.Round(officeGlobal.globalFund/365, 0, MidpointRounding.AwayFromZero);
                    officeGlobal.maxDailyDistributionPerIP = Math.Round(officeGlobal.maxDailyDistribution / (officeGlobal.totalIP /1000) , 4, MidpointRounding.AwayFromZero);
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BuildingManage.OfficeGlobalSummaryGet() : Error on WS calls for balances"));
            }

            return officeGlobal;
        }      

        public List<HistoryProduction> EvalProductionHistory(BuildingHistory buildingHistory, int tokenId, JArray historyRunList, List<ResourceTotal> resourceTotal, bool lastProductionOnly, List<OwnerCitizenExt> ownerCitizenExt)
        {
            bool resourceFlag = false, applianceFlag = false;
            DateTime? eventTimeUTC, lastRunTime = null;
            ResourceTotal currentResource = null;
            List<PlotIP> plotIPList = new();
            List<HistoryProduction> historyProductionList = new();
            PlotDB plotDB = new PlotDB(_context, worldType);
            int sequenceNum = 2;  // first row is reserved for prediction with seq = 1, used for styling of prediction row.

            if (historyRunList != null && historyRunList.Count > 0)
            {
                plotIPList = plotDB.GetIP_Historic(tokenId);

                for (int index = 0; index < historyRunList.Count; index++)
                {
                    JToken historyItem = historyRunList[index];
                    resourceFlag = (historyItem.Value<string>("type") ?? "").Equals("management/resource_produced");
                    applianceFlag = (historyItem.Value<string>("type") ?? "").Equals("management/appliance_produced");

                    // Check if building history relates to current owner (ignore all prior owners history)
                    if ((resourceFlag || applianceFlag) &&
                         (historyItem.Value<string>("to_address") ?? "").ToUpper().Equals(buildingHistory.owner_matic.ToUpper()))
                    {
                        HistoryProduction historyProductionDetails = new();
                        eventTimeUTC = historyItem.Value<DateTime?>("event_time");
                        lastRunTime ??= eventTimeUTC;

                        historyProductionDetails.run_datetime = common.DateStandard(common.TimeFormatStandardFromUTC("", historyItem.Value<DateTime?>("event_time")));
                        historyProductionDetails.run_datetimeDT = ((DateTime)eventTimeUTC).Ticks;       // Ticks send is in UTC, which may differ from display time in GMT

                        JToken historyData = historyItem.Value<JToken>("data");
                        if (historyData != null && historyData.HasValues)
                        {
                            if (resourceFlag)
                            {
                                historyProductionDetails.amount_produced = historyData.Value<int?>("amount") ?? 0;
                                historyProductionDetails.building_product_id = historyData.Value<int?>("resourceId") ?? 0;
                                historyProductionDetails.building_product = GetResourceName(historyProductionDetails.building_product_id);
                            }
                            else // Factory - Appliance
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
                                historyProductionDetails.building_type = historyLand.Value<int?>("building_type_id") ?? 0;
                                historyProductionDetails.building_lvl = historyLand.Value<int?>("building_level") ?? 0;

                                historyProductionDetails = PopulateProductionDetails(historyProductionDetails, ownerCitizenExt);

                                var matchedIP = plotIPList.Where(x => x.last_updated <= (DateTime)eventTimeUTC).OrderByDescending(x => x.last_updated).FirstOrDefault();
                                historyProductionDetails.building_ip = matchedIP == null ? 0 : matchedIP.total_ip < 0 ? 0 : matchedIP.total_ip;     // Check if IP is negative (energy) then set to 0
                                historyProductionDetails.influence_bonus = matchedIP == null ? 0 : matchedIP.influence_bonus ?? 0;

                                historyProductionDetails.poi_bonus = matchedIP == null || matchedIP.production_poi_bonus == null ? 0 : (decimal)matchedIP.production_poi_bonus;
                                //historyProductionDetails.is_perk_activated = matchedIP == null ? false : matchedIP.is_perk_activated ?? false;

                                historyProductionDetails.seq = sequenceNum++;
                            }
                        }

                        historyProductionList.Add(historyProductionDetails);

                        // CHECK if only the last production run needed (nighly sync uses this limited call)
                        if (lastProductionOnly)
                        {
                            break;
                        }
                    }
                }
            }

            return historyProductionList;
        }

        public HistoryProduction FullRecheckCitizens(BuildingHistory buildingHistory, int asset_id, int waitPeriodMS, Plot targetPlot, HistoryProduction historyProduction, List<int> buildingActiveAssignedCitMCP, ref List<OwnerCitizenExt> ownerCitizenExt)
        {
            OwnerCitizenExtDB ownerCitizenExtDB = new(_context);
            CitizenManage citizenManage = new(_context, worldType);
            List<OwnerCitizenExt> removedCitizenList;
            List<int> removedCitizenTokenID = new();
            DateTime checkHistoryStartingFrom;

            if (historyProduction == null || historyProduction.run_datetimeDT == 0)
            {
                checkHistoryStartingFrom = DateTime.UtcNow.AddDays(-1);
            }
            else
            {
                checkHistoryStartingFrom = new DateTime(historyProduction.run_datetimeDT).AddDays(-1);
            }


            // A: Refresh all Citizens currently assigned to building.  Only Get and eval Cit history if current Citizen[Land, Pet, Owner] differs from stored data
            //    NOTE : May not be any Citizens currently assigned, issue with missing citizens from prior run.
            buildingActiveAssignedCitMCP.ForEach(token_id =>
            {
                citizenManage.GetCitizenMCP(token_id, false, checkHistoryStartingFrom).Wait();
            });

            // B: Check if local store has citizens no longer assigned to building (checked earlier via MCP WS)
            List<OwnerCitizenExt> storedActiveCitizens = ownerCitizenExt.Where(x => x.valid_to_date is null).ToList();
            removedCitizenList = storedActiveCitizens.Where(x => !buildingActiveAssignedCitMCP.Contains(x.token_id)).ToList();
            if (removedCitizenList.Count > 0)
            {
                buildingHistory.changes_last_run = AddChangeMessage(string.Concat(removedCitizenList.Count, "x Citizen removed/swapped since last run"), buildingHistory.changes_last_run);
                removedCitizenList.ForEach(cit =>
                {
                    // Update Cit, Only Get and eval Cit history if current Citizen[Land, Pet, Owner] differs from stored data
                    citizenManage.GetCitizenMCP(cit.token_id, false, new DateTime(historyProduction.run_datetimeDT).AddDays(-1)).Wait();
                    removedCitizenTokenID.Add(cit.token_id);
                });
            }

            // C: Check last run Citizen Set, may differ completely from current set. When using speedUp, multiple citizen sets may be swapped out within 24 hr period
            //    Only process citizens not already processed within step A and B above.
            if (historyProduction != null)
            {
                DateTime lastRunTime = new DateTime(historyProduction.run_datetimeDT).AddDays(-1);      // Run a wider sweep of Citizen checks in case of drops due to SpeedUp or DataSync fault. 
                List<OwnerCitizenExt> filterCitizens = ownerCitizenExt.Where(x => (x.valid_to_date >= lastRunTime || x.valid_to_date is null)).ToList();

                filterCitizens.RemoveAll(x => buildingActiveAssignedCitMCP.Contains(x.token_id));
                filterCitizens.RemoveAll(x => removedCitizenTokenID.Contains(x.token_id));

                // List may contain multiple entries for a single Citizen due to wide sweep, no link_date start, action such as Citizens with different pets assigned will get pulled as separte OwnerCitizen rows.
                List<int> citizenTokenList = filterCitizens.Select(r => r.token_id).DistinctBy(r => ((uint)r)).ToList();

                citizenTokenList.ForEach(tokenId =>
                {
                    // Update Cit and history (checking if already stored),  IMPORTANT - if this call occurs multiple times for the same citizen then dups can occur as new link records are not saved to db yet(para2 = false)
                    citizenManage.GetCitizenMCP(tokenId, false, lastRunTime).Wait();
                });
            }

            // D: Commit db changes
            _context.SaveChanges();


            // E: Refresh complete - Get latest (stored) citizens used by building.
            ownerCitizenExtDB.GetBuildingCitizen(asset_id, ref ownerCitizenExt);
            List<OwnerCitizenExt> currentUpdatedCitizenSet = ownerCitizenExt.Where(x => x.valid_to_date is null).ToList();

            // F: Apply refreshed Citizen list for this building due to prior step actions
            if (historyProduction != null)
            {
                PopulateProductionDetails(historyProduction, ownerCitizenExt);
            }

            return historyProduction;
        }

        // Passed historyProduction var is typically partially populated.
        // PopulateProductionDetails()
        //  Usage:
        //  1) GetHistory()  Ln449 >> Refresh last production run with updated citizens(condition: check_citizens = true)
        //  2) EvalProductionHistory()  Ln642 >>  Each historic production run is evaluated
        //  3) FullRecheckCitizens() Ln719 >> After Full citizen refresh, reevaluate passed target production run(either last collection, or not evaluated due to first run)
        public HistoryProduction PopulateProductionDetails(HistoryProduction historyProductionDetails, List<OwnerCitizenExt> ownerCitizenExt)
        {
            DateTime? eventTime = null;
            PetUsage petUsageCompare = null;    // Used to find Pet usage differenc between 2 event dates, using null means do not compare to a prior event date - only use current pets in Cit cals

            if (historyProductionDetails.run_datetimeDT != 0)
            {
                eventTime = (new DateTime(historyProductionDetails.run_datetimeDT)).AddSeconds(-(int)CITIZEN_HISTORY.CORRECTION_SECONDS);  // Assign/Remove Pets may occur before event date is assigned to production run and still be included in run
            }

            historyProductionDetails.efficiency_p = CalculateEfficiency_Production(historyProductionDetails.building_type, historyProductionDetails.building_lvl, historyProductionDetails.amount_produced, historyProductionDetails.building_product_id);
            historyProductionDetails.efficiency_m = CalculateEfficiency_MinMax(historyProductionDetails.building_type, historyProductionDetails.building_lvl, historyProductionDetails.amount_produced, historyProductionDetails.building_product_id);

            historyProductionDetails.efficiency_c_60 = CalculateEfficiency_Citizen(
                ownerCitizenExt,
                eventTime,
                historyProductionDetails.building_lvl,
                historyProductionDetails.building_type,
                (BUILDING_PRODUCT)historyProductionDetails.building_product_id,
                petUsageCompare);

            historyProductionDetails.pet_usage = citizenManage.GetPetUsage(eventTime, ownerCitizenExt);

            historyProductionDetails.efficiency_c = Math.Round(
                historyProductionDetails.efficiency_c_60
                / (historyProductionDetails.building_type == (int)BUILDING_TYPE.ENERGY && historyProductionDetails.building_product_id == (int)BUILDING_PRODUCT.ENERGY ? .45m : .6m),
                1,
                MidpointRounding.AwayFromZero);

            return historyProductionDetails;
        }

        public BuildingHistory GetPrediction(BuildingHistory buildingHistory, HistoryProduction lastProduction, int tokenId, decimal ipEfficiency, DateTime? lastRunTime, List<OwnerCitizenExt> ownerCitizenExt)
        {
            List<string> changeSinceLastRun = new();
            buildingHistory.changes_last_run = null;
            buildingHistory.use_prediction = false;
            buildingHistory.check_citizens = false;

            PlotDB plotDB = new PlotDB(_context, worldType);
            List<Plot> targetPlotList = plotDB.GetPlotbyToken(tokenId);   // MEGA & HUGE return multiple plots.
            Plot targetPlot = targetPlotList[0];
            int targetPlotLevel = targetPlot.building_level;

            //CHECK if pets used in last prod run - then include in predicted next run. Pets may have been removed from cits - temp use is common
            PetUsage petUsageCurrentCits = citizenManage.GetPetUsage(DateTime.Now, ownerCitizenExt);

            buildingHistory.damage = 100 - (targetPlot.condition ?? 0);
            buildingHistory.damage_eff = GetDamageCoeff(buildingHistory.damage);
            buildingHistory.damage_eff_2Place = Math.Round(100 - buildingHistory.damage_eff, 2, MidpointRounding.AwayFromZero);
            buildingHistory.condition_rounded = Math.Round(buildingHistory.damage_eff, 0, MidpointRounding.AwayFromZero);

            buildingHistory.current_building_id = targetPlot.building_id;
            buildingHistory.current_building_type = targetPlot.building_type_id;
            buildingHistory.current_building_product_id = GetBuildingProduct(targetPlot.building_type_id, targetPlot.building_id, targetPlot.action_id, worldType);
            buildingHistory.prediction_product = GetResourceName(buildingHistory.current_building_product_id);

            buildingHistory.prediction_base_min = GetBaseProduce(targetPlot.building_type_id, targetPlotLevel, buildingHistory.current_building_product_id);
            buildingHistory.prediction_max = GetMaxProduce(targetPlot.building_type_id, targetPlotLevel, buildingHistory.current_building_product_id);
            buildingHistory.prediction_range = buildingHistory.prediction_max - buildingHistory.prediction_base_min;
            buildingHistory.current_building_lvl = targetPlotLevel;


            // prediction.prediction_ip_doublebug = (int)Math.Round((decimal)targetPlot.influence * (decimal)(1 + ((evalIPbonus *2) / 100.0)), 0, MidpointRounding.AwayFromZero);
            // Standard Prediction evaluation
            Prediction prediction = GetPredictionData(targetPlotList, buildingHistory, ipEfficiency, ownerCitizenExt, lastProduction);
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

            if (buildingHistory.damage_eff == 0)
            {
                prediction.total = 0;
                changeSinceLastRun.Add(string.Concat("Building operation stopped due to damage, Repair Required!"));
            }

            // Check if citizen uses temp pets, if prediction is lower then actual last run & Building not upgraded & IP unchanged & POI Bonus unchanged
            // Example Ispera or Returner : X282 Y70,  add & remove pet per run - within 4 mins period, need to check each cit history and insert db records so history can calcuate Cit eff% correctly per run.
            if (lastProduction != null &&
                targetPlotLevel == lastProduction.building_lvl)
            {
                changeSinceLastRun.AddRange(GetPetChanges(petUsageCurrentCits, lastProduction.pet_usage));
            }

            //if (new DateTime(lastProduction.run_datetimeDT) < DateTime.Now.AddDays(-7))
            //{
            //    changeSinceLastRun.Add("Last run > 7 days ago, wont be included in Prediction eval due to age");
            //}

            buildingHistory.prediction = prediction;

            // Dont save last run amount_produced to plot db IF   (this plot will then not be included in the prediction eval feature) 
            // 1) building was upgraded since last run - as this last produce value is used in IP league prediction eval.
            // 2) double produce occured due to staked perk, dont include in prediction eval
            // 3) double produce runs might not exceed max produce due to low cit and IP eff%, so remove outliers where produce is >50% predicted.
            // 4) predicted Citizen efficiency is 0% - meaning no cits currently assigned.
            // 5) Citizen changed from last run (Cit efficiency change), future run & prediction will change to reflect current cits assigned - dont use in eval if diff.
            // 6) If IP used by Prediction differs from last run IP, then dont use in eval.
            if (lastProduction == null)
            {
                buildingHistory.use_prediction = true;

                if (!changeSinceLastRun.Any())
                {
                    changeSinceLastRun.Add("None found");
                }
            }
            else if (targetPlotLevel == lastProduction.building_lvl &&
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

        Prediction GetPredictionData(List<Plot> targetPlotList, BuildingHistory buildingHistory, decimal ipEfficiency, List<OwnerCitizenExt> ownerCitizenExt, HistoryProduction lastProduction)
        {
            Prediction prediction = new();
            Plot targetPlot = targetPlotList[0];
            int evalIPbonus = targetPlot.influence_bonus ?? 0;
            int buildingType = targetPlot.building_type_id;

            prediction.product_id = GetBuildingProduct(buildingType, targetPlot.building_id, targetPlot.action_id, worldType);
            prediction.influance = targetPlot.influence ?? 0;   // Not Used in calc - only show in Building History module, may not match actual calculated IP - plot.influence_info (2022)
            prediction.influance_bonus = evalIPbonus;

            // USE influence_info as this is more accurate using calculation of all building influance, rather then the building.influence with may be stale showing prior influance when POI/Mon is deactivated
            // CHECK if influance_info is neg IP, then set to min of 0
            prediction.ip = targetPlot.influence_info > 0 ? (int)Math.Round((decimal)targetPlot.influence_info * (decimal)(1 + (evalIPbonus / 100.0)), 0, MidpointRounding.AwayFromZero) : 0;

            //prediction.is_perk_activated = targetPlot.is_perk_activated ?? false; // NOTE THIS ATTRIBUTE IS NOT WORKING (not filled by MCP) - CAN REMOVE LATER

            // Get Cit efficiency using currently assigned cits (may differ from last run) before produce calc                            
            prediction.cit_efficiency_partial = CalculateEfficiency_Citizen(ownerCitizenExt, DateTime.Now, targetPlot.building_level, buildingType, (BUILDING_PRODUCT)prediction.product_id, lastProduction == null ? null : lastProduction.pet_usage);
            prediction.cit_range_percent = prediction.product_id == (int)BUILDING_PRODUCT.ENERGY ? 45 : 60;
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

                prediction.resource_range_percent = prediction.product_id == (int)BUILDING_PRODUCT.ENERGY ? 25 : 20;
                prediction.resource_lvl = targetPlotAbundance;
                prediction.resource_lvl_percent = prediction.product_id == (int)BUILDING_PRODUCT.ENERGY ? GetElectricResouceLevel(prediction.resource_lvl) : GetWaterResouceLevel(prediction.resource_lvl);
                prediction.resource_partial = (prediction.resource_range_percent / 100.0m) * prediction.resource_lvl_percent;


                prediction.resource_lvl_range = buildingHistory.prediction_range * (prediction.resource_range_percent / 100m);
                prediction.resource_lvl_produce = (decimal)(buildingHistory.prediction_range *
                    (prediction.resource_range_percent / 100m) *
                    (prediction.resource_lvl_percent / 100.0m));

                prediction.resource_lvl_produce_rounded = (int)Math.Round(prediction.resource_lvl_produce, 0, MidpointRounding.AwayFromZero);
            }

            // Water : 20% , ENERGY : 30%, REST : 40%
            prediction.ip_range_percent = prediction.product_id == (int)BUILDING_PRODUCT.ENERGY ? 30 : prediction.product_id == (int)BUILDING_PRODUCT.WATER ? 20 : 40;
            prediction.ip_efficiency = ipEfficiency;
            prediction.ip_efficiency_partial = (prediction.ip_efficiency / 100.0m) * prediction.ip_range_percent;
            prediction.ip_efficiency_rounded = (int)Math.Round(prediction.ip_efficiency_partial, 0, MidpointRounding.AwayFromZero);

            prediction.ip_produce = (prediction.ip_efficiency / 100.0m) * (prediction.ip_range_percent / 100m) * buildingHistory.prediction_range;
            prediction.ip_produce_rounded = Math.Round(prediction.ip_produce, 2, MidpointRounding.AwayFromZero);
            prediction.ip_produce_max = (prediction.ip_range_percent / 100.0m) * buildingHistory.prediction_range;

            // GOLDEN Rule: COMBINE IP and Cit % , then round to 0 places, then multiple against effective range.
            prediction.ip_and_cit_percent_100 = prediction.ip_efficiency_partial + prediction.cit_efficiency_partial + prediction.resource_partial;
            prediction.ip_and_cit_percent_rounded = Math.Round(prediction.ip_and_cit_percent_100, 0, MidpointRounding.AwayFromZero);

            prediction.ip_and_cit_percent_dmg = Math.Round(prediction.ip_and_cit_percent_rounded * (buildingHistory.condition_rounded / 100m), 2, MidpointRounding.AwayFromZero);
            prediction.ip_and_cit_percent_dmg_rounded = (int)Math.Round(prediction.ip_and_cit_percent_dmg, 0, MidpointRounding.AwayFromZero);

            prediction.ip_and_cit_produce_dmg = (prediction.ip_and_cit_percent_dmg_rounded / 100.0m) * buildingHistory.prediction_range;
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
            prediction.total_decimal_100 = prediction.total_decimal_100 > buildingHistory.prediction_max ? buildingHistory.prediction_max : prediction.total_decimal_100;
            prediction.total_100 = (int)Math.Round(
                    prediction.total_decimal_100,
                    0, MidpointRounding.AwayFromZero);

            prediction.total_decimal_100 = Math.Round(prediction.total_decimal_100, 2, MidpointRounding.AwayFromZero);   // For UI only



            // Corner Cases (a)cant be higher then max - bonus may place higher calc (b)no cits assigned
            prediction.total = prediction.total > buildingHistory.prediction_max ? buildingHistory.prediction_max : prediction.total;


            prediction.efficiency_p = CalculateEfficiency_Production(buildingType, targetPlot.building_level, prediction.total, prediction.product_id);
            prediction.efficiency_m = CalculateEfficiency_MinMax(buildingType, targetPlot.building_level, prediction.total, prediction.product_id);

            return prediction;
        }

        private RETURN_CODE UpdateResourceTotal(List<ResourceTotal> resourceTotal, int buildingType, BuildingTypeIP building, int buildingLvl, WORLD_TYPE worldType)
        {
            ResourceTotal currentResource = null;
            Building buildingUtl = new();

            BUILDING_PRODUCT produce = building.building_id switch
            {
                (int)BUILDING_SUBTYPE.FACTORY => BUILDING_PRODUCT.FACTORY_PRODUCT,
                (int)BUILDING_SUBTYPE.BRICKWORKS => BUILDING_PRODUCT.BRICK,
                (int)BUILDING_SUBTYPE.GLASSWORKS => BUILDING_PRODUCT.GLASS,
                (int)BUILDING_SUBTYPE.CONCRETE_PLANT => worldType switch
                {
                    WORLD_TYPE.TRON => BUILDING_PRODUCT.CONCRETE,
                    WORLD_TYPE.ETH => BUILDING_PRODUCT.STEEL,
                    WORLD_TYPE.BNB => BUILDING_PRODUCT.PLASTIC,
                    _ => BUILDING_PRODUCT.CONCRETE
                },
                (int)BUILDING_SUBTYPE.PAPER_FACTORY => BUILDING_PRODUCT.PAPER,
                (int)BUILDING_SUBTYPE.CHEMICAL_PLANT => worldType switch
                {
                    WORLD_TYPE.TRON => BUILDING_PRODUCT.MIXES,
                    WORLD_TYPE.ETH => BUILDING_PRODUCT.GLUE,
                    WORLD_TYPE.BNB => BUILDING_PRODUCT.COMPOSITE,
                    _ => BUILDING_PRODUCT.MIXES
                },
                (int)BUILDING_SUBTYPE.POWER_PLANT => BUILDING_PRODUCT.ENERGY,
                (int)BUILDING_SUBTYPE.WATER_PLANT => BUILDING_PRODUCT.WATER,
                (int)BUILDING_SUBTYPE.APARTMENTS => BUILDING_PRODUCT.CITIZEN_PRODUCTION_APT,
                (int)BUILDING_SUBTYPE.CONDOMINIUM => BUILDING_PRODUCT.CITIZEN_PRODUCTION_CONDO,
                (int)BUILDING_SUBTYPE.VILLA => BUILDING_PRODUCT.CITIZEN_PRODUCTION_VILLA,
                (int)BUILDING_SUBTYPE.OFFICE_BLOCK => BUILDING_PRODUCT.MEGA_PRODUCT_LOCAL,                
                (int)BUILDING_SUBTYPE.BUSINESS_CENTER => BUILDING_PRODUCT.MEGA_PRODUCT_GLOBAL,
                (int)BUILDING_SUBTYPE.TRADE_CENTER => BUILDING_PRODUCT.COMMERCIAL_SERVICE,
                (int)BUILDING_SUBTYPE.SUPERMARKET => BUILDING_PRODUCT.COMMERCIAL_SERVICE,
                (int)BUILDING_SUBTYPE.POLICE => BUILDING_PRODUCT.INSURANCE_COVER_POLICE,
                (int)BUILDING_SUBTYPE.HOSPITAL => BUILDING_PRODUCT.INSURANCE_COVER_HOSPITAL,
                (int)BUILDING_SUBTYPE.FIRE_STATION => BUILDING_PRODUCT.INSURANCE_COVER_FIRESTATION,
                _ => (BUILDING_PRODUCT)building.last_run_produce_id
            };

            // Building may have previously produced a product that is produced in the current building type - eg Lvl1 mixer industry Changed to lvl1 brick production.
            if (CheckProduct(buildingType, produce, building.building_id))
            {
                // Find Stored Resource match, and increment
                if (resourceTotal.Count > 0)
                {
                    currentResource = (ResourceTotal)resourceTotal.Where(row => row.resourceId == (int)produce).FirstOrDefault();
                }
                if (currentResource == null)
                {
                    currentResource = new();
                    currentResource.resourceId = (int)produce;
                    currentResource.name = produce == BUILDING_PRODUCT.FACTORY_PRODUCT ? "Factory" : GetResourceName((int)produce);
                    currentResource.buildingId = building.building_id;
                    currentResource.buildingImg = building.building_img;
                    currentResource.buildingName = GetBuildingNameShort(building.building_id, worldType);
                    currentResource.buildingSeq = GetBuildingNameOrder(building.building_id, worldType);
                    resourceTotal.Add(currentResource);
                }

                // ACTIVE BUILDING (Last 9 days produced) - Add building last run produce details - including if active or shutdown
                if (buildingType == (int)BUILDING_TYPE.OFFICE || buildingType == (int)BUILDING_TYPE.COMMERCIAL ||
                    (
                    building.last_run_produce_id == (int)produce &&
                    building.last_run_produce != null &&
                    building.last_run_produce > 0 &&
                    building.last_run_produce_date >= DateTime.Now.AddDays(-(int)ACTIVE_BUILDING.DAYS)))
                {
                    currentResource.total += building.last_run_produce == null ? 0 : (long)building.last_run_produce;
                    currentResource.buildingCount++;

                    // Active Building Require (a) min citizen count assigned to building  (b) building condition > min 10%
                    if (building.active_stamina && building.condition > 10)
                    {
                        building.active_building = true;
                        currentResource.buildingActive++;
                        currentResource.buildingActiveIP += building.total_ip;
                    }
                    else
                    {
                        building.active_building = false;                        
                    }
                }
                else
                {
                    currentResource.buildingCount++;
                    building.active_building = false;
                }

                if (building.condition > 10 && building.citizen_count > 0 && building.active_stamina == false)
                {
                    building.out_of_statina_alert = true;
                }
            }

            return RETURN_CODE.SUCCESS;
        }

        // Check building has min set of citizens assigned
        public bool CheckBuildingActive(BUILDING_TYPE buildingType, int citizenCount, int buildingLvl, int buildingTokenID)
        {
            bool active = false;
            CitizenManage citizenManage = new(_context, worldType);

            active = buildingLvl switch
            {
                1 => citizenCount >= 1,
                2 => citizenCount >= 2,
                3 => citizenCount >= 4,
                4 => citizenCount >= 6,
                5 => citizenCount >= 8,
                6 => citizenCount >= 11,
                7 => citizenCount >= 14,
                _ => false
            };

            // Check building citizens have min stamina + min set of active citizens
            if (active)
            {
                active = citizenManage.CheckMinStaminaBuilding(buildingTokenID, buildingLvl, buildingType);
            }

            return active;
        }

        public bool CheckProductiveBuildingType(BUILDING_TYPE buildingType)
        {
            return buildingType == BUILDING_TYPE.ENERGY || buildingType == BUILDING_TYPE.INDUSTRIAL || buildingType == BUILDING_TYPE.PRODUCTION;
        
        }

        // lastActionUx (Building) : Used to identify collection time, on collection event, last_action is updated
        //                           Updates also triggered on : Citizen change, Industry production change, building transfer. It is not trigger on: building Repair, or IP change
        public ProductionCollection CollectionEval(BUILDING_TYPE buildingType, int buildingLevel, double lastActionUx)
        {
            ProductionCollection productionCollection = new();
            
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            System.DateTime dtLastAction = dtDateTime.AddSeconds((double)lastActionUx);

            System.DateTime dtNextCollect = buildingLevel switch
            {
                1 => dtLastAction.AddDays(7),
                2 => dtLastAction.AddDays(6),
                3 => dtLastAction.AddDays(5),
                4 => dtLastAction.AddDays(4),
                5 => dtLastAction.AddDays(3),
                6 => dtLastAction.AddDays(2),
                7 => dtLastAction.AddDays(1),
                _ => dtLastAction.AddDays(7)
            };


            productionCollection.ready = dtNextCollect <= DateTime.UtcNow;
            TimeSpan timediff = dtNextCollect - DateTime.UtcNow;
            productionCollection.day = timediff.Days;
            productionCollection.hour = ((int)timediff.TotalHours - (productionCollection.day * 24));

            productionCollection.minutes = (int)timediff.TotalMinutes;
            if (timediff.TotalSeconds > 0 && timediff.TotalSeconds < 60)
            {
                productionCollection.minutes = 1;
            }

            return productionCollection;
        }

        private bool CheckProduct(int buildingType, BUILDING_PRODUCT produceId, int buildingId)
        {
            if (buildingType == (int)BUILDING_TYPE.PRODUCTION &&
               (produceId == BUILDING_PRODUCT.BRICK
                 || produceId == BUILDING_PRODUCT.CONCRETE
                 || produceId == BUILDING_PRODUCT.STEEL
                 || produceId == BUILDING_PRODUCT.PLASTIC
                 || produceId == BUILDING_PRODUCT.GLASS
                 || produceId == BUILDING_PRODUCT.PAPER
                 || produceId == BUILDING_PRODUCT.MIXES
                 || produceId == BUILDING_PRODUCT.GLUE
                 || produceId == BUILDING_PRODUCT.COMPOSITE
                 || buildingId == (int)BUILDING_SUBTYPE.FACTORY))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.INDUSTRIAL &&
                (produceId == BUILDING_PRODUCT.SAND ||
                  produceId == BUILDING_PRODUCT.STONE ||
                  produceId == BUILDING_PRODUCT.WOOD ||
                  produceId == BUILDING_PRODUCT.METAL
                ))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.ENERGY &&
                (produceId == BUILDING_PRODUCT.ENERGY ||
                  produceId == BUILDING_PRODUCT.WATER))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.OFFICE &&
                (produceId == BUILDING_PRODUCT.MEGA_PRODUCT_GLOBAL || produceId == BUILDING_PRODUCT.MEGA_PRODUCT_LOCAL))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.COMMERCIAL &&
               (produceId == BUILDING_PRODUCT.COMMERCIAL_SERVICE))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.RESIDENTIAL &&
                (produceId == BUILDING_PRODUCT.CITIZEN_PRODUCTION_CONDO || produceId == BUILDING_PRODUCT.CITIZEN_PRODUCTION_VILLA || produceId == BUILDING_PRODUCT.CITIZEN_PRODUCTION_APT))
            {
                return true;
            }
            else if (buildingType == (int)BUILDING_TYPE.MUNICIPAL &&
                (produceId == BUILDING_PRODUCT.INSURANCE_COVER_POLICE || produceId == BUILDING_PRODUCT.INSURANCE_COVER_FIRESTATION || produceId == BUILDING_PRODUCT.INSURANCE_COVER_HOSPITAL))
            {
                return true;
            }

            return false;
        }

        // RULE: Damage >=90 points has 0% DamageCoeff,  buildings stops producing/working at 90 or higher damage points.
        public decimal GetDamageCoeff(int damage)
        {
            decimal damageCoeff = 100;

            // https://www.dcode.fr/function-equation-finder
            // Using Parabola / Hyperbola using curve fitting
            // f(x) = −0.00773426x2 − 0.419696x + 100.215 
            if (damage > 0 && damage <90)
            {
                damageCoeff = 0.00773426m * (decimal)(damage * damage);
                damageCoeff += 0.419696m * damage;
                damageCoeff = 100.215m - damageCoeff;
            }
            else if (damage >= 90)
            {
                damageCoeff = 0;
            }

            return damageCoeff;
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
                        change = string.Concat("Applied ", type, " pet(s) used in Last run : ", diff > 0 ? "+" : "", diff, " (Pet Currently not assigned)");
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

        public static int GetBuildingProduct(int buildingTypeId, int buildingId, int actionId, WORLD_TYPE worldType)
        {
            // ETH WORLD ENH needed to support Steel,  uses same buildingId(9) but product is steel (need to have some check on current client system active)
            // Industry building with mult type product - plot.action_id stores product currently being made / or if inactive the last product made.  On switch of product type - action_id is updated
            return buildingTypeId == (int)BUILDING_TYPE.INDUSTRIAL ? actionId :
                buildingId switch
                {
                    (int)BUILDING_SUBTYPE.BRICKWORKS => (int)BUILDING_PRODUCT.BRICK,
                    (int)BUILDING_SUBTYPE.GLASSWORKS => (int)BUILDING_PRODUCT.GLASS,
                    (int)BUILDING_SUBTYPE.CONCRETE_PLANT => worldType switch
                    {
                        WORLD_TYPE.TRON => (int)BUILDING_PRODUCT.CONCRETE,
                        WORLD_TYPE.ETH => (int)BUILDING_PRODUCT.STEEL,
                        WORLD_TYPE.BNB => (int)BUILDING_PRODUCT.PLASTIC,
                        _ => (int)BUILDING_PRODUCT.CONCRETE
                    },
                    (int)BUILDING_SUBTYPE.PAPER_FACTORY => (int)BUILDING_PRODUCT.PAPER,
                    (int)BUILDING_SUBTYPE.CHEMICAL_PLANT => worldType switch
                    {
                        WORLD_TYPE.TRON => (int)BUILDING_PRODUCT.MIXES,
                        WORLD_TYPE.ETH => (int)BUILDING_PRODUCT.GLUE,
                        WORLD_TYPE.BNB => (int)BUILDING_PRODUCT.COMPOSITE,
                        _ => (int)BUILDING_PRODUCT.MIXES
                    },
                    (int)BUILDING_SUBTYPE.POWER_PLANT => (int)BUILDING_PRODUCT.ENERGY,
                    (int)BUILDING_SUBTYPE.WATER_PLANT => (int)BUILDING_PRODUCT.WATER,
                    (int)BUILDING_SUBTYPE.FACTORY => (int)BUILDING_PRODUCT.FACTORY_PRODUCT,

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
                    totalFormat = String.Format("{0:n0}", resourceTotal[count].total * multiplier * (7m / (8m - buildingLevel)))
                });
            }

            return formatedTotal;
        }


        private int CheckBuildingCitizenHistory(List<OwnerCitizenExt> citizens, DateTime eventDate, int buildingTokenId, string ownerMatic, int waitPeriodMS = 100)
        {
            CitizenManage citizenManage = new(_context, worldType);
            DBLogger dbLogger = new(_context, worldType);
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
            msgCollection = msgCollection == null ? new string[] { message } : msgCollection.Append(message);

            return msgCollection;
        }

        public async Task<JArray> GetPoiMCP(List<int> tokenIdList)
        {
            string content = string.Empty;
            //JToken jsonToken = null;
            JArray poiArray = null;
            int retryCount = 0;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            while (returnCode == RETURN_CODE.ERROR && retryCount < 5)
            {
                try
                {
                    retryCount++;
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.POI_GET, WORLD_TYPE.BNB => BNB_WS.POI_GET, WORLD_TYPE.ETH => ETH_WS.POI_GET, _ => TRON_WS.LAND_GET };

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        // using tokenIdList.Select() allows for the string manipulation per item within the source tokenId list, matching the WS parameter JSON spec
                        StringContent stringContent = new StringContent("{\"token_ids\": [" +
                            string.Join(",", tokenIdList.Select(x => "\"" + x.ToString() + "\""))
                            + "]}"
                            , Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, "");

                    if (content.Length != 0)
                    {
                        poiArray = JArray.Parse(content);
                        //if (poiArray != null && poiArray.Count > 0)
                        //{
                        //    jsonToken = poiArray[0];
                        //}
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    if (_context != null)
                    {
                        _context.LogEvent(String.Concat("BuildingManage::GetPoiMCP() : Error getting POI active_until data"));
                        _context.LogEvent(ex.Message);
                    }
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("BuildingManage::GetPoiMCP() : retry successful - no ", retryCount));
                }
            }

            return poiArray;
        }

        private int AssignTaxMatch(District district, int buildingType)
        {
            int produceTax = 0;

            if (buildingType == (int)BUILDING_TYPE.INDUSTRIAL)
            {
                produceTax = district.construction_industry_production_tax ?? 0;
            }

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    produceTax = district.citizen_tax ?? 0;
                    break;
                case (int)BUILDING_TYPE.ENERGY:
                    produceTax = district.energy_tax ?? 0;
                    break;
                case (int)BUILDING_TYPE.OFFICE:
                case (int)BUILDING_TYPE.COMMERCIAL:
                    produceTax = district.commercial_tax ?? 0;
                    break;
                case (int)BUILDING_TYPE.INDUSTRIAL:
                case (int)BUILDING_TYPE.PRODUCTION:
                    produceTax = district.production_tax ?? 0;
                    break;

                default:
                    produceTax = 0;
                    break;
            }

            return produceTax;
        }

        // Citizen Efficiency calc per building uses following rules
        // Calculation evaluated on sum all citizens triats, not line by line per cit, impacts rounding.
        // Only one rounding rule applied - rounded to 2 places as final rule - after all other calcs - rounding on efficiency % (not produce here).
        // Calucate efficiency % as partial related to building type (eg 60% or 45%) and not 100% as shown in Citizen modules for easy eval - to get x/100% convert the partial.
        private decimal CalculateEfficiency_Citizen(List<OwnerCitizenExt> citizens, DateTime? eventDate, int? buildingLevel, int buildingType, BUILDING_PRODUCT buildingProduct, PetUsage petUsageCompare)
        {
            decimal efficiency = 0;
            PetUsage petUsageOnEventDate = citizenManage.GetPetUsage(eventDate, citizens);
            PetUsage petUsageDifference = new();

            // Only use last Production run pets - if no pets currently assigned
            if (CheckPetUsage(petUsageOnEventDate) == false)
            {
                if (petUsageCompare != null)
                {
                    petUsageDifference = petUsageCompare;
                }
                else
                {
                    petUsageDifference = petUsageOnEventDate;
                }
            }
            else
            {
                petUsageDifference = petUsageOnEventDate;
            }

            // Identify any Pet usage difference between last run and current cits assigned to building.
            // Note that if a Pet is already assigned to a cit, then the cit traits will reflect it - dont double the Pet bonus.
            //petUsageDifference.agility = petUsageCompare != null && petUsageCompare.agility > petUsageOnEventDate.agility ? petUsageCompare.agility - petUsageOnEventDate.agility : petUsageOnEventDate.agility;
            //petUsageDifference.charisma = petUsageCompare != null && petUsageCompare.charisma > petUsageOnEventDate.charisma ? petUsageCompare.charisma - petUsageOnEventDate.charisma : petUsageOnEventDate.charisma;
            //petUsageDifference.endurance = petUsageCompare != null && petUsageCompare.endurance > petUsageOnEventDate.endurance ? petUsageCompare.endurance - petUsageOnEventDate.endurance : petUsageOnEventDate.endurance;
            //petUsageDifference.intelligence = petUsageCompare != null && petUsageCompare.intelligence > petUsageOnEventDate.intelligence ? petUsageCompare.intelligence - petUsageOnEventDate.intelligence : petUsageOnEventDate.intelligence;
            //petUsageDifference.luck = petUsageCompare != null && petUsageCompare.luck > petUsageOnEventDate.luck ? petUsageCompare.luck - petUsageOnEventDate.luck : petUsageOnEventDate.luck;
            //petUsageDifference.strength = petUsageCompare != null && petUsageCompare.strength > petUsageOnEventDate.strength ? petUsageCompare.strength - petUsageOnEventDate.strength : petUsageOnEventDate.strength;                


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
                    (filterCitizens.Sum(x => x.trait_agility + x.trait_charisma + x.trait_intelligence + x.trait_luck)
                        + petUsageDifference.agility + petUsageDifference.charisma + petUsageDifference.intelligence + petUsageDifference.luck) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }
            else if (buildingType == (int)BUILDING_TYPE.PRODUCTION)
            {
                efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength) + petUsageDifference.strength) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_endurance + x.trait_charisma + x.trait_intelligence + x.trait_luck)
                        + petUsageDifference.endurance + petUsageDifference.charisma + petUsageDifference.intelligence + +petUsageDifference.luck) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }
            else if (buildingType == (int)BUILDING_TYPE.ENERGY)
            {
                if (buildingProduct == BUILDING_PRODUCT.WATER)
                {
                    efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_endurance) + petUsageDifference.endurance) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength + x.trait_charisma + x.trait_intelligence + x.trait_luck)
                        + petUsageDifference.strength + petUsageDifference.charisma + petUsageDifference.intelligence + petUsageDifference.luck) / 4.0 * .1 / citizenMax
                    , 3) * 10;
                }
                else if (buildingProduct == BUILDING_PRODUCT.ENERGY)
                {
                    //  NOTE Energy using a coefficient of .45 of min-max output versus 60% for all others.
                    efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_endurance) + petUsageDifference.endurance) * .25 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_agility) + petUsageDifference.agility) * .15 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_strength + x.trait_charisma + x.trait_intelligence + x.trait_luck)
                        + petUsageDifference.strength + petUsageDifference.charisma + petUsageDifference.intelligence + petUsageDifference.luck) / 4.0 * .05 / citizenMax
                    , 3) * 10;
                }
            }
            else if (buildingType == (int)BUILDING_TYPE.OFFICE)
            {
                efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_intelligence) + petUsageDifference.intelligence) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_charisma) + petUsageDifference.charisma) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_endurance + x.trait_agility + x.trait_strength + x.trait_luck)
                        + petUsageDifference.endurance + petUsageDifference.agility + petUsageDifference.strength + petUsageDifference.luck) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }
            else if (buildingType == (int)BUILDING_TYPE.COMMERCIAL)
            {
                efficiency = filterCitizens.Count == 0 ? 0 : (decimal)Math.Round(
                    (filterCitizens.Sum(x => x.trait_charisma) + petUsageDifference.charisma) * .3 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_luck) + petUsageDifference.luck) * .2 / citizenMax +
                    (filterCitizens.Sum(x => x.trait_endurance + x.trait_agility + x.trait_strength + x.trait_intelligence)
                        + petUsageDifference.endurance + petUsageDifference.agility + petUsageDifference.strength + petUsageDifference.intelligence) / 4.0 * .1 / citizenMax
                    , 3) * 10;
            }

            return efficiency;
        }

        private bool CheckPetUsage(PetUsage petUsage)
        {

            return petUsage.agility > 0 ||
                petUsage.strength > 0 ||
                petUsage.intelligence > 0 ||
                petUsage.luck > 0 ||
                petUsage.charisma > 0 ||
                petUsage.endurance > 0;
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
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE || buildingProduct == (int)BUILDING_PRODUCT.PLASTIC)
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
                    else if (buildingProduct == (int)BUILDING_PRODUCT.MIXES || buildingProduct == (int)BUILDING_PRODUCT.COMPOSITE || buildingProduct == (int)BUILDING_PRODUCT.GLUE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_CHEMICAL.LEVEL_7,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.PAPER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_1,
                            2 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_2,
                            3 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_3,
                            4 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_4,
                            5 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_5,
                            6 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_6,
                            7 => (amount_produced * 100d) / (int)MAX_PAPER.LEVEL_7,
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }
            return (int)Math.Round(Double.IsNaN(efficiency) ? 0 : efficiency);
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
                    (int)BUILDING_PRODUCT.PLASTIC => buildingLvl switch
                    {
                        1 => (int)MIN_PLASTIC.LEVEL_1,
                        2 => (int)MIN_PLASTIC.LEVEL_2,
                        3 => (int)MIN_PLASTIC.LEVEL_3,
                        4 => (int)MIN_PLASTIC.LEVEL_4,
                        5 => (int)MIN_PLASTIC.LEVEL_5,
                        6 => (int)MIN_PLASTIC.LEVEL_6,
                        7 => (int)MIN_PLASTIC.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.MIXES or (int)BUILDING_PRODUCT.GLUE or (int)BUILDING_PRODUCT.COMPOSITE => buildingLvl switch
                    {
                        1 => (int)MIN_CHEMICAL.LEVEL_1,
                        2 => (int)MIN_CHEMICAL.LEVEL_2,
                        3 => (int)MIN_CHEMICAL.LEVEL_3,
                        4 => (int)MIN_CHEMICAL.LEVEL_4,
                        5 => (int)MIN_CHEMICAL.LEVEL_5,
                        6 => (int)MIN_CHEMICAL.LEVEL_6,
                        7 => (int)MIN_CHEMICAL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.PAPER => buildingLvl switch
                    {
                        1 => (int)MIN_PAPER.LEVEL_1,
                        2 => (int)MIN_PAPER.LEVEL_2,
                        3 => (int)MIN_PAPER.LEVEL_3,
                        4 => (int)MIN_PAPER.LEVEL_4,
                        5 => (int)MIN_PAPER.LEVEL_5,
                        6 => (int)MIN_PAPER.LEVEL_6,
                        7 => (int)MIN_PAPER.LEVEL_7,
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
                    (int)BUILDING_PRODUCT.PLASTIC => buildingLvl switch
                    {
                        1 => (int)MAX_PLASTIC.LEVEL_1,
                        2 => (int)MAX_PLASTIC.LEVEL_2,
                        3 => (int)MAX_PLASTIC.LEVEL_3,
                        4 => (int)MAX_PLASTIC.LEVEL_4,
                        5 => (int)MAX_PLASTIC.LEVEL_5,
                        6 => (int)MAX_PLASTIC.LEVEL_6,
                        7 => (int)MAX_PLASTIC.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.MIXES or (int)BUILDING_PRODUCT.GLUE or (int)BUILDING_PRODUCT.COMPOSITE => buildingLvl switch
                    {
                        1 => (int)MAX_CHEMICAL.LEVEL_1,
                        2 => (int)MAX_CHEMICAL.LEVEL_2,
                        3 => (int)MAX_CHEMICAL.LEVEL_3,
                        4 => (int)MAX_CHEMICAL.LEVEL_4,
                        5 => (int)MAX_CHEMICAL.LEVEL_5,
                        6 => (int)MAX_CHEMICAL.LEVEL_6,
                        7 => (int)MAX_CHEMICAL.LEVEL_7,
                        _ => 0
                    },
                    (int)BUILDING_PRODUCT.PAPER => buildingLvl switch
                    {
                        1 => (int)MAX_PAPER.LEVEL_1,
                        2 => (int)MAX_PAPER.LEVEL_2,
                        3 => (int)MAX_PAPER.LEVEL_3,
                        4 => (int)MAX_PAPER.LEVEL_4,
                        5 => (int)MAX_PAPER.LEVEL_5,
                        6 => (int)MAX_PAPER.LEVEL_6,
                        7 => (int)MAX_PAPER.LEVEL_7,
                        _ => 0
                    },
                    _ => 0
                },

                _ => 0
            };

        }

        // Use: Calc efficiency % between min and max range, based on passed produce product - unit amount.
        private static int CalculateEfficiency_MinMax(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
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
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE || buildingProduct == (int)BUILDING_PRODUCT.PLASTIC)
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
                    else if (buildingProduct == (int)BUILDING_PRODUCT.MIXES || buildingProduct == (int)BUILDING_PRODUCT.GLUE || buildingProduct == (int)BUILDING_PRODUCT.COMPOSITE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_1) * 100d / ((int)MAX_CHEMICAL.LEVEL_1 - (int)MIN_CHEMICAL.LEVEL_1),
                            2 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_2) * 100d / ((int)MAX_CHEMICAL.LEVEL_2 - (int)MIN_CHEMICAL.LEVEL_3),
                            3 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_3) * 100d / ((int)MAX_CHEMICAL.LEVEL_3 - (int)MIN_CHEMICAL.LEVEL_3),
                            4 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_4) * 100d / ((int)MAX_CHEMICAL.LEVEL_4 - (int)MIN_CHEMICAL.LEVEL_4),
                            5 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_5) * 100d / ((int)MAX_CHEMICAL.LEVEL_5 - (int)MIN_CHEMICAL.LEVEL_5),
                            6 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_6) * 100d / ((int)MAX_CHEMICAL.LEVEL_6 - (int)MIN_CHEMICAL.LEVEL_6),
                            7 => (amount_produced - (int)MIN_CHEMICAL.LEVEL_7) * 100d / ((int)MAX_CHEMICAL.LEVEL_7 - (int)MIN_CHEMICAL.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.PAPER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_PAPER.LEVEL_1) * 100d / ((int)MAX_PAPER.LEVEL_1 - (int)MIN_PAPER.LEVEL_1),
                            2 => (amount_produced - (int)MIN_PAPER.LEVEL_2) * 100d / ((int)MAX_PAPER.LEVEL_2 - (int)MIN_PAPER.LEVEL_3),
                            3 => (amount_produced - (int)MIN_PAPER.LEVEL_3) * 100d / ((int)MAX_PAPER.LEVEL_3 - (int)MIN_PAPER.LEVEL_3),
                            4 => (amount_produced - (int)MIN_PAPER.LEVEL_4) * 100d / ((int)MAX_PAPER.LEVEL_4 - (int)MIN_PAPER.LEVEL_4),
                            5 => (amount_produced - (int)MIN_PAPER.LEVEL_5) * 100d / ((int)MAX_PAPER.LEVEL_5 - (int)MIN_PAPER.LEVEL_5),
                            6 => (amount_produced - (int)MIN_PAPER.LEVEL_6) * 100d / ((int)MAX_PAPER.LEVEL_6 - (int)MIN_PAPER.LEVEL_6),
                            7 => (amount_produced - (int)MIN_PAPER.LEVEL_7) * 100d / ((int)MAX_PAPER.LEVEL_7 - (int)MIN_PAPER.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }

            // RULE : efficiency can not be negative
            efficiency = efficiency < 0 ? 0 : efficiency;

            return (int)Math.Round(Double.IsNaN(efficiency) ? 0 : efficiency);
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
                (int)APPLICATION_ID.RED_SAT => "Red Sat",    // Red Sat (7) = 10% bonus
                (int)APPLICATION_ID.WHITE_SAT => "White Sat",     // White Sat
                (int)APPLICATION_ID.RED_AIR_CON => "Red Air Con",     // White Air con
                (int)APPLICATION_ID.GREEN_AIR_CON => "Green Air Con",     // Green Air con
                (int)APPLICATION_ID.WHITE_AIR_CON => "White Air Con",     // White Air con
                (int)APPLICATION_ID.CCTV_RED => "CCTV Red",     // CCTV RED 
                (int)APPLICATION_ID.CCTV_WHITE => "CCTV White",     // CCTV White                            
                (int)APPLICATION_ID.ROUTER_BLACK => "Router Black",     // Router Black 
                (int)APPLICATION_ID.RED_FIRE_ALARM => "Red Fire Alarm",     // Red Fire Alarm
                _ => "Product"
            };
        }

        private string GetResourceName(int? buildingProduct)
        {
            return buildingProduct switch
            {
                (int)BUILDING_PRODUCT.WOOD => "Wood",
                (int)BUILDING_PRODUCT.SAND => "Sand",
                (int)BUILDING_PRODUCT.STONE => "Stone",
                (int)BUILDING_PRODUCT.METAL => "Metal",
                (int)BUILDING_PRODUCT.BRICK => "Brick",
                (int)BUILDING_PRODUCT.GLASS => "Glass",
                (int)BUILDING_PRODUCT.CONCRETE => "Concrete",
                (int)BUILDING_PRODUCT.STEEL => "Steel",
                (int)BUILDING_PRODUCT.PLASTIC => "Plastic",
                (int)BUILDING_PRODUCT.WATER => "Water",
                (int)BUILDING_PRODUCT.ENERGY => "Energy",
                (int)BUILDING_PRODUCT.FACTORY_PRODUCT => "Factory Product",
                (int)BUILDING_PRODUCT.PAPER => "Paper",
                (int)BUILDING_PRODUCT.MIXES => "Mixes",
                (int)BUILDING_PRODUCT.COMPOSITE => "Composite",
                (int)BUILDING_PRODUCT.GLUE => "Glue",

                _ => "Product"
            };
        }

        private string GetBuildingNameShort(int buildingId, WORLD_TYPE worldType)
        {
            return buildingId switch
            {
                (int)BUILDING_SUBTYPE.SMELTER_PLANT => "Smelter",
                (int)BUILDING_SUBTYPE.MIXING_PLANT => "Mixing",
                (int)BUILDING_SUBTYPE.POWER_PLANT => "Power",
                (int)BUILDING_SUBTYPE.WATER_PLANT => "Water",
                (int)BUILDING_SUBTYPE.GLASSWORKS => "Glass",
                (int)BUILDING_SUBTYPE.BRICKWORKS => "Brick",
                (int)BUILDING_SUBTYPE.CHEMICAL_PLANT => "Chemical",
                (int)BUILDING_SUBTYPE.PAPER_FACTORY => "Paper",
                (int)BUILDING_SUBTYPE.FACTORY => "Factory",
                (int)BUILDING_SUBTYPE.CONCRETE_PLANT => worldType switch
                {
                    WORLD_TYPE.TRON => "Concrete",
                    WORLD_TYPE.ETH => "Steel",
                    WORLD_TYPE.BNB => "Plastic",
                    _ => "Concrete",
                },
                (int)BUILDING_SUBTYPE.VILLA => "Villa",
                (int)BUILDING_SUBTYPE.CONDOMINIUM => "Condo",
                (int)BUILDING_SUBTYPE.APARTMENTS => "Apart",
                (int)BUILDING_SUBTYPE.POLICE => "Police",
                (int)BUILDING_SUBTYPE.HOSPITAL => "Hospital",
                (int)BUILDING_SUBTYPE.SUPERMARKET => "Market",
                (int)BUILDING_SUBTYPE.OFFICE_BLOCK => "Office Block",
                (int)BUILDING_SUBTYPE.BUSINESS_CENTER => "Biz Center",
                _ => ""
            };
        }
        private int GetBuildingNameOrder(int buildingId, WORLD_TYPE worldType)
        {
            return buildingId switch
            {
                (int)BUILDING_SUBTYPE.SMELTER_PLANT => 1,
                (int)BUILDING_SUBTYPE.MIXING_PLANT => 2,
                (int)BUILDING_SUBTYPE.POWER_PLANT => 1,
                (int)BUILDING_SUBTYPE.WATER_PLANT => 2,
                (int)BUILDING_SUBTYPE.GLASSWORKS => 2,
                (int)BUILDING_SUBTYPE.BRICKWORKS => 1,
                (int)BUILDING_SUBTYPE.CHEMICAL_PLANT => 4,
                (int)BUILDING_SUBTYPE.PAPER_FACTORY => 3,
                (int)BUILDING_SUBTYPE.FACTORY => 6,
                (int)BUILDING_SUBTYPE.CONCRETE_PLANT => worldType switch
                {
                    WORLD_TYPE.TRON => 5,
                    WORLD_TYPE.ETH => 5,
                    WORLD_TYPE.BNB => 5,
                    _ => 5,
                },
                (int)BUILDING_SUBTYPE.VILLA => 1,
                (int)BUILDING_SUBTYPE.CONDOMINIUM => 2,
                (int)BUILDING_SUBTYPE.APARTMENTS => 3,
                (int)BUILDING_SUBTYPE.POLICE => 1,
                (int)BUILDING_SUBTYPE.HOSPITAL => 2,
                (int)BUILDING_SUBTYPE.SUPERMARKET => 3,
                (int)BUILDING_SUBTYPE.OFFICE_BLOCK => 1,
                (int)BUILDING_SUBTYPE.BUSINESS_CENTER => 2,
                _ => 0
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
               (buildingLevel == 7 && lastRunProduceDate < DateTime.Now.AddDays(-1)))
            {
                result = true;
            }

            return result;
        }

        // Find Standard Deviation from passed list,  deviation of -1 to 1 = 68.2% of all results.
        // https://www.statisticshowto.com/probability-and-statistics/standard-deviation/
        private double StandardDeviationGet(List<int> elements)
        {

            // A) Get (Sum)Squared / count
            var A = Math.Pow(elements.Sum(), 2) / elements.Count;

            var B = 0.0;
            for (int index = 0; index < elements.Count; index++)
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

        // IP changed from db store, then update ranking (used by Plot Full and Partial updates - as shown in MyPortfolio)
        public decimal CheckInfluenceRankChange(int newInfluence, int storedInfluence, int influenceBonus, decimal storedRanking, int buildingLevel, int buildingType, int targetBuildingTokenId, int buildingId, int districtId, string buidingOwnerMatic)
        {
            decimal newRanking = storedRanking;
            int maxRankingIp = 0, minRankingIp = 0, rangeIP = 0, targetBuildingTotalIp = 0;
            List<Plot> buildingPlotList = null;
            BuildingRanking buildingRanking = new();            

            // Change detected, evalute ranking.
            if (newInfluence != storedInfluence && (buildingType == (int)BUILDING_TYPE.ENERGY || buildingType == (int)BUILDING_TYPE.INDUSTRIAL || buildingType == (int)BUILDING_TYPE.PRODUCTION))
            {
                // dont include current building within the matching ranking building list, as (scenario) its old IP value may have been min and now its latest IP is the max - this would skew the min-max values
                buildingPlotList = _context.plot.Where(x => x.building_level == buildingLevel && x.building_type_id == buildingType && x.token_id != targetBuildingTokenId).ToList();

                // If first plot for this type and level (and not yet updated on local) then set with max ranking.
                if (buildingPlotList.Count <= 1)
                {
                    newRanking = 100;
                }
                else
                {
                    targetBuildingTotalIp = GetInfluenceTotal(newInfluence, influenceBonus);

                    maxRankingIp = buildingRanking.CalcMaxIP(buildingPlotList);
                    maxRankingIp = maxRankingIp < targetBuildingTotalIp ? targetBuildingTotalIp : maxRankingIp;         // Assign current building IP as MAX  - if above stored building collection max

                    minRankingIp = buildingRanking.CalcMinIP(buildingPlotList);                    
                    minRankingIp = minRankingIp > targetBuildingTotalIp ? targetBuildingTotalIp : minRankingIp;         // Assign current building IP as MIN  - if below stored building collection min

                    rangeIP = maxRankingIp - minRankingIp;

                    newRanking = buildingRanking.GetIPEfficiency(targetBuildingTotalIp, rangeIP, minRankingIp, _context);
                }

                CheckBuildingAlert(targetBuildingTokenId, buidingOwnerMatic, storedRanking, newRanking, buildingType, buildingLevel, buildingId, districtId);
            }

            return newRanking;
        }

        public RETURN_CODE CheckBuildingAlert(int tokenId, string ownerMatic, decimal storedRanking, decimal newRanking, int buildingType, int buildingLevel, int buildingId, int districtId)
        {
            List<Database.AlertTrigger> ownerAlerts = new();
            AlertTriggerManager alertTrigger = new(_context, worldType);
            AlertManage alertManage = new(_context, worldType);
            Building building = new();

            ownerAlerts = alertTrigger.GetByType("ALL", ALERT_TYPE.BUILDING_RANKING, tokenId);

            ownerAlerts.ToList().ForEach(x =>
            {
                alertManage.AddRankingAlert(x.matic_key, ownerMatic, tokenId, storedRanking, newRanking, buildingLevel, building.BuildingType(buildingType, buildingId), districtId, (ALERT_TYPE)x.key_type);
            });

            return RETURN_CODE.SUCCESS;
        }
    }



}
