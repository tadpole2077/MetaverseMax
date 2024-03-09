using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text;
using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using System.Net.Http.Json;
using static Azure.Core.HttpHeader;
using System.Reflection.Metadata;
using Microsoft.IdentityModel.Tokens;

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

        // LIMITATION CONCERN : 2023/08/13
        // SCENARIO : Plot was upgraded to Huge or Mega within same day from empty plot, identifed and processed within nightly run.
        //            Related plots wont be picked up and updated, and will remain lost (need to find all such plots in this status).
        //            Solution needed to handle this scenario.
        // PARCELS  : parcel plots are not included within MCP land WS  (building collection per owner matic) - but parcel plots are procesed during ALL nightly sync's
        //
        public Plot AddOrUpdatePlot(int plotId, int posX, int posY, bool saveEvent, bool forceMissionCheck = false)
        {
            Plot plotMatched = null;
            PlotDB plotDB = new(_context);
            JObject jsonContent, jsonContentParcel = null;            
            List<int> citizenList = new();
            JArray citizenArray, jsonContentBuildingUnit = null;
            int plotCount = 0, parcelId = 0, parcelInfoId = 0;

            try
            {
                jsonContent = Task.Run(() => GetPlotMCP(posX, posY)).Result;

                // If plotId is passed, then posX and posY not needed. Indicates plot already exists based on table key
                if (jsonContent != null)
                {
                    parcelId = jsonContent.Value<int?>("parcel_id") ?? 0;
                    if (parcelId > 0)
                    {
                        // NOTE : Parcal plots have owner_avatar_id=0 and owner_nickname = ''  [Rule Handled in OwnerManage sproc's and code]
                        jsonContentParcel = Task.Run(() => GetParcelMCP(parcelId)).Result;

                        JObject parcelInfo = jsonContentParcel.Value<JObject>("info");
                        parcelInfoId = parcelInfo == null ? 0 : parcelInfo.Value<int?>("id") ?? 0;          // Used to retrieve the custom building image
                    }

                    plotMatched = AddOrUpdatePlotData(jsonContent, jsonContentParcel, posX, posY, plotId, false, forceMissionCheck);       // Save to db as batch at end - due to related building plots

                    if (parcelInfoId > 0)
                    {
                        jsonContentBuildingUnit = Task.Run(() => GetBuildingUnitMCP(parcelId)).Result;

                        // WARNING MUST Save changes (commit to db) after each add/update of custom, as multi plot custom may cause ' instance with the same key value is already been tracked issue'
                        AddOrUpdateCustomBuilding(parcelInfoId, jsonContentParcel, jsonContentBuildingUnit);
                        _context.SaveChanges();
                    }

                    // Find Citizen token_ids currently assigned to plot - used by features such as ranking.  plotMatched is returned by method.
                    citizenArray = jsonContent.Value<JArray>("citizens") ?? new();
                    plotMatched.citizen = citizenArray.Select(c => (c.Value<int?>("id") ?? 0)).ToList();
                

                    // Special Cases : update related building plots (Huge, Mega, Custom, Parcel)
                    //  Huge updated to MEGA - the other huge will also get processed and its paired plot should also get updated.
                    //  MEGA or HUGE distroyed -  all related building plots will be reset to empty, on next nightly sync will be picked up as new buildings (if built)                   
                    if (plotMatched.building_level == 6 || plotMatched.building_level == 7 || plotMatched.parcel_id > 0)
                    {
                        // Update each related plot for this building - Safer to do this in code vs sproc - due to deadlock/concurrent updates
                        //    Issue where master plot gets updated first, then another distinct action updates the plot details (eg. user load IP Ranking page)
                        //    Solution creates a more autonomous unit insuring that identical set of data is updated for a building.
                        plotCount = plotDB.UpdateRelatedBuildingPlotLocal(plotMatched);
                        //plotDB.UpdateRelatedBuildingPlotSproc(plotMatched.plot_id);

                        // CORNER CASE: Not all matching plots found and updated. POSSIBLE new token and huge/mega built on same day
                        // 
                        if (plotMatched.upgradedSinceLastSync == false && (plotCount != 1 && plotMatched.building_level == 6) || (plotCount != 3 && plotMatched.building_level == 7)) {
                            _context.LogEvent(String.Concat("PlotManage::AddOrUpdatePlot() : Anomoly found - Building ", plotMatched.token_id, ", building does not have correct amount of plots assigned."));
                        }
                    }
                }

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("PlotManage:AddOrUpdatePlot() : Error MCP WS get Plot X:", posX, " Y:", posY));                
            }

            return plotMatched;
        }

        public RETURN_CODE AddOrUpdateCustomBuilding(int parcelInfoId, JObject jsonContentParcel, JArray jsonContentBuildingUnit)
        {
            Building building = new();
            CustomBuildingDB customBuildingDB = new(_context, worldType);
            JObject parcelInfo = jsonContentParcel.Value<JObject>("info");
            
            int buildingCategoryId = 0;
            string buildingName = string.Empty;
            int unitOnSaleCount = 0, largeSize = 0, smallSize = 0;
            decimal highCoin = 0, lowCoin = 0, highMega = 0, lowMega = 0, megaPrice = 0, coinPrice = 0;

            CustomBuilding customBuilding = customBuildingDB.GetBuildingByInfoId(parcelInfoId);


#nullable enable
            buildingName = parcelInfo == null ? string.Empty : parcelInfo.Value<string?>("name") ?? "";
#nullable disable

            buildingCategoryId = parcelInfo == null ? 0 : parcelInfo.Value<int?>("categoryId") ?? 0;
            // Special Case : Main tower shops - null categoryId assigned - using placeholder of 100
            if (buildingCategoryId == 0 && parcelInfo != null && buildingName == "Main Tower")
            {
                buildingCategoryId = 100;
            }
            
            foreach(JObject unit in jsonContentBuildingUnit)
            {
                if (unit.Value<bool?>("on_sale") ?? false)
                {
                    unitOnSaleCount++;
                    int size = unit.Value<int?>("area") ?? 0;
                    largeSize = size > largeSize ? size: largeSize;
                    smallSize = (size < smallSize && size !=0) || smallSize == 0 ? size : smallSize;

                    JObject saleData = unit.Value<JObject>("sale_data") ?? null;
                    megaPrice = (saleData.Value<decimal?>("sellMegaPrice") ?? 0) / 1000000000000000000;      // 18 decimal place source reduction
                    coinPrice = building.convertPrice(saleData.Value<decimal?>("sellPrice") ?? 0, worldType);

                    highCoin = coinPrice > highCoin ? coinPrice : highCoin;
                    lowCoin = (coinPrice < lowCoin && coinPrice != 0 )|| lowCoin == 0 ? coinPrice : lowCoin;
                    highMega = megaPrice > highMega ? megaPrice : highMega;
                    lowMega = (megaPrice < lowMega && megaPrice != 0 )|| lowMega == 0? megaPrice : lowMega;
                }
            }
            
            if (customBuilding == null)
            {
                customBuildingDB.Add(new()
                {
                    parcel_info_id = parcelInfoId,
                    parcel_unit_count = parcelInfo == null ? 0 : parcelInfo.Value<int?>("unitsCount") ?? 0,
                    building_category_id = buildingCategoryId,
                    building_name = buildingName,
                    floor_count = parcelInfo == null ? 0 : parcelInfo.Value<int?>("floors") ?? 0,
                    unit_forsale_count = unitOnSaleCount,
                    unit_price_high_coin = highCoin,
                    unit_price_low_coin = lowCoin == 0 ? highCoin : lowCoin,
                    unit_price_high_mega = highMega,
                    unit_price_low_mega = lowMega == 0 ? highMega : lowMega,
                    unit_sale_largest_size = largeSize,
                    unit_sale_smallest_size = smallSize == 0 ? largeSize : smallSize,
                });
            }
            else
            {
                customBuilding.parcel_unit_count = parcelInfo == null ? 0 : parcelInfo.Value<int?>("unitsCount") ?? 0;
                customBuilding.building_category_id = buildingCategoryId;
                customBuilding.building_name = buildingName;
                customBuilding.floor_count = parcelInfo == null ? 0 : parcelInfo.Value<int?>("floors") ?? 0;
                customBuilding.unit_forsale_count = unitOnSaleCount;
                customBuilding.unit_price_high_coin = highCoin;
                customBuilding.unit_price_low_coin = lowCoin == 0 ? highCoin : lowCoin;
                customBuilding.unit_price_high_mega = highMega;
                customBuilding.unit_price_low_mega = lowMega == 0 ? highMega : lowMega;
                customBuilding.unit_sale_largest_size = largeSize;
                customBuilding.unit_sale_smallest_size = smallSize == 0 ? largeSize : smallSize;

                customBuildingDB.Update(customBuilding);
            }

            return RETURN_CODE.SUCCESS;
        }

        public Plot AddOrUpdatePlotData(JObject jsonContent, JObject jsonContentParcel, int posX, int posY, int plotId, bool saveEvent, bool forceMissionCheck)
        {
            PlotDB plotDB = new(_context, worldType);
            ServiceClass.ServiceCommon common = new();
            Plot plotMatched = null;
            CitizenManage citizen = new(_context, worldType);
            BuildingManage buildingManage = new(_context, worldType);
            Building building = new();
            EVENT_TYPE lastActionType = EVENT_TYPE.UNKNOWN;
            int newInfluence = 0;
            int parcelId = 0, parcelTypeId = 0, parcelInfoIdNew = 0, parcelInfoIdOld = 0, parcelUnitCount = 0, building_category_id = 0, parcelOwnerAvatarId = 0;
            bool parcelOnSale = false;
            decimal parcelPrice = 0;
            string parcelOwner = string.Empty, buildingName = string.Empty, parcelOwnerNickname = string.Empty;
            DateTime? parcelLastAction = null;
            decimal balance = 0;

            try
            {
                // Based on the callers passed plotId, either add a new plot or update an existing plot record.
                if (plotId == 0)
                {
                    // Defensive check Plot may already exist - error occured during initial world sync - plot dropped.
                    plotMatched = plotDB.GetPlotbyPosXPosY(posX, posY);

                    if (plotMatched != null)
                    {
                        _context.LogEvent(String.Concat("PlotDB:AddOrUpdatePlot() : Existing plot was found for X:", posX, " Y:", posY, ". This maybe unexpected! Call was to create new plot at these XY coord. Existing Plot will be updated"));
                    }
                }
                // Existing claimed plot
                else
                {
                    plotMatched = _context.plot.Find(plotId);                    
                }


                // Parcel owner,nickname,avatarId are not stored with plot dataset (nickname and avatorId also not found with unbuild parcel) : as of 2023-08
                if (jsonContentParcel != null)
                {
                    OwnerManage ownerManage = new OwnerManage(_context, worldType);
                    parcelId = jsonContentParcel.Value<int>("id");

                    parcelTypeId = jsonContentParcel.Value<int?>("token_type") ?? 0;
                    parcelOnSale = jsonContentParcel.Value<bool?>("on_sale") ?? false;
                    parcelPrice = parcelOnSale ? building.convertPrice(jsonContentParcel.Value<decimal?>("price") ?? 0, worldType) : 0;

                    // parcel/get WS returns a checksum wallet key (Upper and lower case), as we historically use only lower case - apply lowercase conversion.
                    parcelOwner = jsonContentParcel.Value<string>("address").ToLower();                   

                    OwnerAccount ownerAccount = ownerManage.GetOwnerAccountByMatic(parcelOwner);
                    parcelOwnerNickname = ownerAccount != null ? ownerAccount.name : "TBA";
                    parcelOwnerAvatarId = ownerAccount != null ? ownerAccount.avatar_id : 0;

                    JObject parcelInfo = jsonContentParcel.Value<JObject>("info");
                    parcelInfoIdNew = parcelInfo == null ? 0 : parcelInfo.Value<int?>("id") ?? 0;          // Used to retrieve the custom building image
                    parcelUnitCount = parcelInfo == null ? 0 : parcelInfo.Value<int?>("unitsCount") ?? 0;
                    building_category_id = parcelInfo == null ? 0 : parcelInfo.Value<int?>("categoryId") ?? 0;
#nullable enable
                    buildingName = parcelInfo == null ? string.Empty : parcelInfo.Value<string?>("name") ?? "";
#nullable disable
                    // Special Case : Main tower shops - null categoryId assigned - using placeholder of 100
                    if (building_category_id == 0 && parcelInfo != null && buildingName == "Main Tower")
                    {
                        building_category_id = 100;
                    }

                    parcelLastAction = parcelInfo != null ? parcelInfo.Value<DateTime?>("updatedAt") ?? null : null;  //TimeFormatStandardFromUTC(sourceTime, dtSourceTime)
                    TokenHistory tokenHistory = new(_context, worldType);
                    TokenLastAction tokenLastAction = tokenHistory.GetLastAction(parcelId, TOKEN_TYPE.PARCEL);

                    lastActionType = tokenLastAction.eventType;
                    parcelLastAction = tokenLastAction.eventTime;

                }


                // Based on the callers passed plotId, either add a new plot or update an existing plot record.
                if (plotId == 0 && plotMatched == null)
                {
                    plotMatched = plotDB.AddPlot(new Plot()
                    {
                        update_type = (int)UPDATE_TYPE.FULL,
                        pos_x = posX,
                        pos_y = posY,
                        cell_id = jsonContent.Value<int?>("cell_id") ?? 0,
                        district_id = jsonContent.Value<int?>("region_id") ?? 0,
                        land_type = jsonContent.Value<int?>("land_type") ?? 0,

                        last_action_type = (int)lastActionType,
                        last_action = parcelLastAction ?? ServiceCommon.UnixTimeStampUTCToDateTime(jsonContent.Value<int?>("last_action"), null),
                        last_updated = DateTime.UtcNow,
                        unclaimed_plot = string.IsNullOrEmpty(jsonContent.Value<string>("owner")),

#nullable enable
                        owner_nickname = parcelId == 0 ? CheckNameLength(jsonContent.Value<string?>("owner_nickname") ?? "") : parcelOwnerNickname,
#nullable disable                                                
                        owner_matic = parcelId == 0 ? jsonContent.Value<string>("owner").ToLower() : parcelOwner,            // parcelOwner must be used if assigned, as when a parcel - plot.owner is a system owner.
                        owner_avatar_id = parcelId == 0 ? jsonContent.Value<int>("owner_avatar_id") : parcelOwnerAvatarId,

                        on_sale = parcelId == 0 ?
                            jsonContent.Value<bool?>("on_sale") ?? false : parcelOnSale,
                        current_price = parcelId == 0 ?
                            jsonContent.Value<bool?>("on_sale") ?? false ? building.GetSalePrice(jsonContent.Value<JToken>("sale_data"), worldType) : 0 : parcelPrice,

                        resources = jsonContent.Value<int?>("resources") ?? 0,
                        building_id = jsonContent.Value<int?>("building_id") ?? 0,
                        building_level = jsonContent.Value<int?>("building_level") ?? 0,
                        building_type_id = parcelTypeId > 0 ? parcelTypeId : jsonContent.Value<int?>("building_type_id") ?? 0,
                        token_id = jsonContent.Value<int?>("token_id") ?? 0,

                        for_rent = (jsonContent.Value<int?>("for_rent") ?? 0) > 0 ? building.GetRentPrice(jsonContent.Value<JToken>("rent_info"), worldType) : 0,
                        rented = jsonContent.Value<string>("renter") != null,
                        abundance = jsonContent.Value<int?>("abundance") ?? 0,
                        building_abundance = 0,
                        condition = parcelId == 0 ? jsonContent.Value<int?>("condition") ?? 0 : 100,

                        influence_info = plotDB.GetInfluenceInfoTotal(jsonContent.Value<JToken>("influence_info"), jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false, posX, posY, jsonContent.Value<int?>("building_type_id") ?? 0),
                        influence = jsonContent.Value<int?>("influence") ?? 0,
                        influence_bonus = jsonContent.Value<int?>("influence_bonus") ?? 0,
                        influence_poi_bonus = jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false,
                        production_poi_bonus = jsonContent.Value<decimal?>("production_poi_bonus") ?? 0.0m,
                        is_perk_activated = jsonContent.Value<Boolean?>("is_perk_activated") ?? false,
                        app_4_bonus = plotDB.GetApplicationBonus(4, jsonContent.Value<JArray>("extra_appliances"), posX, posY),
                        app_5_bonus = plotDB.GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY),
                        app_123_bonus = plotDB.GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY),
                        current_influence_rank = buildingManage.CheckInfluenceRankChange(jsonContent.Value<int?>("influence") ?? 0, 0, jsonContent.Value<int?>("influence_bonus") ?? 0, 0, jsonContent.Value<int?>("building_level") ?? 0, jsonContent.Value<int?>("building_type_id") ?? 0, jsonContent.Value<int?>("token_id") ?? 0, jsonContent.Value<int?>("building_id") ?? 0, jsonContent.Value<int?>("region_id") ?? 0, jsonContent.Value<string>("owner")),

                        citizen_count = jsonContent.Value<JArray>("citizens") == null ? 0 : jsonContent.Value<JArray>("citizens").Count,
                        low_stamina_alert = citizen.CheckCitizenStamina(jsonContent.Value<JArray>("citizens"), jsonContent.Value<int?>("building_type_id") ?? 0),
                        action_id = jsonContent.Value<int?>("action_id") ?? 0,

                        predict_produce = 0,
                        last_run_produce_id = 0,
                        last_run_produce_predict = false,

                        parcel_id = parcelId,
                        parcel_info_id = parcelInfoIdNew,

                    });
                }


                // Existing claimed plot - found by plotId OR posx - posy coordinates
                else if (plotMatched != null)
                {
                    plotMatched.update_type = (int)UPDATE_TYPE.FULL;
                    plotMatched.last_updated = DateTime.UtcNow;
                    plotMatched.last_action_type = (int)lastActionType;

                    plotMatched.unclaimed_plot = string.IsNullOrEmpty(jsonContent.Value<string>("owner"));

                    // parcelOwner Matic key must be used if assigned, when a plot is part of a parcel THEN plot.owner is a system owner.
                    if (parcelId == 0)
                    {
#nullable enable
                        plotMatched.owner_nickname = CheckNameLength(jsonContent.Value<string?>("owner_nickname") ?? "");
#nullable disable       
                        plotMatched.owner_matic = jsonContent.Value<string>("owner");
                        plotMatched.owner_avatar_id = jsonContent.Value<int>("owner_avatar_id");

                        plotMatched.on_sale = jsonContent.Value<bool?>("on_sale") ?? false;
                        plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(jsonContent.Value<JToken>("sale_data"), worldType) : 0;
                        plotMatched.building_type_id = jsonContent.Value<int?>("building_type_id") ?? 0;
                        plotMatched.condition = jsonContent.Value<int?>("condition") ?? 0;
                        plotMatched.last_action = ServiceCommon.UnixTimeStampUTCToDateTime(jsonContent.Value<double?>("last_action"), null);
                    }
                    else
                    {
                        plotMatched.owner_matic = parcelOwner;
                        plotMatched.owner_nickname = parcelOwnerNickname;
                        plotMatched.owner_avatar_id = parcelOwnerAvatarId;

                        plotMatched.on_sale = parcelOnSale;
                        plotMatched.current_price = parcelPrice;
                        plotMatched.building_type_id = parcelTypeId;
                        plotMatched.condition = 100;
                        plotMatched.last_action = parcelLastAction;
                    }

                    plotMatched.resources = jsonContent.Value<int?>("resources") ?? 0;
                    plotMatched.building_id = jsonContent.Value<int?>("building_id") ?? 0;
                    plotMatched.upgradedSinceLastSync = plotMatched.building_level != (jsonContent.Value<int?>("building_level") ?? 0);     // Internal flag used to manage new huge/Mega building now containing potentially many plots
                    plotMatched.building_level = jsonContent.Value<int?>("building_level") ?? 0;
                    plotMatched.token_id = jsonContent.Value<int?>("token_id") ?? 0;

                    plotMatched.for_rent = (jsonContent.Value<int?>("for_rent") ?? 0) > 0 ? building.GetRentPrice(jsonContent.Value<JToken>("rent_info"), worldType) : 0;
                    plotMatched.rented = jsonContent.Value<string>("renter") != null;
                    plotMatched.abundance = jsonContent.Value<int?>("abundance") ?? 0;
                    plotMatched.building_abundance = plotMatched.building_abundance;        // only update from BuildingManage class calc


                    plotMatched.influence_info = plotDB.GetInfluenceInfoTotal(jsonContent.Value<JToken>("influence_info"), jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false, posX, posY, jsonContent.Value<int?>("building_type_id") ?? 0);

                    newInfluence = jsonContent.Value<int?>("influence") ?? 0;
                    plotMatched.influence_bonus = jsonContent.Value<int?>("influence_bonus") ?? 0;
                    plotMatched.current_influence_rank = buildingManage.CheckInfluenceRankChange(newInfluence, plotMatched.influence ?? 0, plotMatched.influence_bonus ?? 0, plotMatched.current_influence_rank ?? 0, plotMatched.building_level, plotMatched.building_type_id, plotMatched.token_id, plotMatched.building_id, plotMatched.district_id, plotMatched.owner_matic);
                    plotMatched.influence = newInfluence;                                   // Placed after ranking check, as both old and new influence needed for check

                    plotMatched.influence_poi_bonus = jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false;
                    plotMatched.production_poi_bonus = jsonContent.Value<decimal?>("production_poi_bonus") ?? 0.0m;
                    plotMatched.is_perk_activated = jsonContent.Value<Boolean?>("is_perk_activated") ?? false;
                    plotMatched.app_4_bonus = plotDB.GetApplicationBonus(4, jsonContent.Value<JArray>("extra_appliances"), posX, posY);
                    plotMatched.app_5_bonus = plotDB.GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY);
                    plotMatched.app_123_bonus = plotDB.GetApplication123Bonus(jsonContent.Value<JToken>("appliances"), posX, posY);

                    plotMatched.citizen_count = jsonContent.Value<JArray>("citizens") == null ? 0 : jsonContent.Value<JArray>("citizens").Count;
                    plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(jsonContent.Value<JArray>("citizens"), plotMatched.building_type_id);
                    plotMatched.action_id = jsonContent.Value<int?>("action_id") ?? 0;

                    plotMatched.predict_produce = plotMatched.predict_produce == null ? 0 : plotMatched.predict_produce;

                    plotMatched.parcel_id = parcelId;
                    parcelInfoIdOld = plotMatched.parcel_info_id;
                    plotMatched.parcel_info_id = parcelInfoIdNew;
                    
                    balance = building.convertPriceMega(jsonContent.Value<decimal?>("balance") ?? 0); 
                    
                    if (balance > 0 || forceMissionCheck)
                    {
                        //Plot building has a Mission balance - worth checking mission to update.
                        JObject buildingMission = Task.Run(() => GetBuildingMissionMCP(plotMatched.token_id)).Result;
                        MissionDB missionDB = new(_context, worldType);
                        missionDB.AddOrUpdate(buildingMission, plotMatched.token_id, balance);
                    }
                }

                // New Custom Building - Add alert
                if (parcelInfoIdNew > 0 && parcelInfoIdOld != parcelInfoIdNew)
                {
                    AddNewBuildingAlert(parcelOwnerNickname, parcelOwner, plotMatched.token_id, plotMatched.district_id, parcelInfoIdNew, buildingName);
                }


                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context, _context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotDB:AddOrUpdatePlot() : Error Adding/update Plot X:", posX, " Y:", posY));
            }

            return plotMatched;
        }

        private string CheckNameLength(string name)
        {
            return name[0..(name.Length > 50 ? 50 : name.Length)];
        }

        public RETURN_CODE AddNewBuildingAlert(string parcelOwnerNickname, string parcelOwner, int tokenId, int districtId, int parcelInfoId, string buildingName)
        {
            AlertTriggerManager alertTrigger = new(_context, worldType);
            AlertManage alert = new(_context, worldType);
            List<AlertTrigger> allOwnerAlerts = new();
            allOwnerAlerts = alertTrigger.GetByType("ALL", ALERT_TYPE.NEW_BUILDING, 0);

            allOwnerAlerts.ForEach(x =>
            {
                alert.AddNewBuildingAlert(x.matic_key, parcelOwnerNickname == string.Empty ? parcelOwner : parcelOwnerNickname, tokenId, districtId, parcelInfoId, buildingName);
            });

            return RETURN_CODE.SUCCESS;
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

        public async Task<JArray> GetBuildingUnitMCP(int parcelId)
        {
            string content = string.Empty;
            JArray jsonContent = null;
            int retryCount = 0;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            while (returnCode == RETURN_CODE.ERROR && retryCount < 5)
            {
                try
                {
                    retryCount++;
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.BUILDING_UNIT_GET, WORLD_TYPE.BNB => BNB_WS.BUILDING_UNIT_GET, WORLD_TYPE.ETH => ETH_WS.BUILDING_UNIT_GET, _ => TRON_WS.BUILDING_UNIT_GET };

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {

                        response = await client.GetAsync(string.Concat(serviceUrl, parcelId.ToString()));

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Concat(" id:", parcelId));

                    if (content.Length != 0)
                    {
                        jsonContent = JArray.Parse(content);
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("PlotManage::GetBuildingUnitMCP() : Error MCP WS get Parcel:", parcelId));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetBuildingUnitMCP() : retry successful - no ", retryCount));
                }
            }

            return jsonContent;
        }

        public async Task<JObject> GetBuildingMissionMCP(int tokenId)
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
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.MISSION, WORLD_TYPE.BNB => BNB_WS.MISSION, WORLD_TYPE.ETH => ETH_WS.MISSION, _ => TRON_WS.MISSION };

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {

                        response = await client.GetAsync(string.Concat(serviceUrl, tokenId.ToString()));

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Concat(" id:", tokenId));

                    if (content.Length != 0)
                    {
                        jsonContent = JObject.Parse(content);
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("PlotManage::GetBuildingMissionMCP() : Error MCP WS get Parcel:", tokenId));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetBuildingUnitMCP() : retry successful - no ", retryCount));
                }
            }

            return jsonContent;
        }

        // Parcel_id is not unique across worlds 
        // Parcel_info_id is unique identifier across world
        public async Task<JObject> GetParcelMCP(int parcelId)
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
                    serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.PARCEL_GET, WORLD_TYPE.BNB => BNB_WS.PARCEL_GET, WORLD_TYPE.ETH => ETH_WS.PARCEL_GET, _ => TRON_WS.PARCEL_GET };

                    // POST REST WS
                    HttpResponseMessage response;
                    using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                    {
                        StringContent stringContent = new StringContent("{\"id\": \"" + parcelId.ToString() + "\"}", Encoding.UTF8, "application/json");

                        response = await client.PostAsync(
                            serviceUrl,
                            stringContent);

                        response.EnsureSuccessStatusCode(); // throws if not 200-299
                        content = await response.Content.ReadAsStringAsync();

                    }
                    watch.Stop();
                    servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, string.Concat(" id:", parcelId));

                    if (content.Length != 0)
                    {
                        jsonContent = JObject.Parse(content);
                    }

                    returnCode = RETURN_CODE.SUCCESS;
                }
                catch (Exception ex)
                {
                    DBLogger dBLogger = new(_context.worldTypeSelected);
                    dBLogger.logException(ex, String.Concat("PlotManage::GetParcelMCP() : Error MCP WS get Parcel:", parcelId));
                }
            }

            if (retryCount > 1 && returnCode == RETURN_CODE.SUCCESS)
            {
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PlotManage::GetParcelMCP() : retry successful - no ", retryCount));
                }
            }

            return jsonContent;
        }

        // Get only one plot for Mega or Huge building - only need to process 1 on nightly sync (NOTE might be an issue if MEGA/HUGE building is destroyed)
        // CORNER CASE : Demolished L6 or L7 need all their related plots full process check - as each plot reverts to a new token on demolish(except for 1 that retains the old token_id) - list of demolished buildings in fullProcessTokenIdList
        public int RemoveMegaHugePlot(List<Plot> plotList, List<int> fullProcessTokenIdList)
        {
            int megaHugeplotsRemoved = plotList.Count;
            List<Plot> filteredPlotsMegaHuge = plotList.Where(x => x.building_level == 7 || x.building_level == 6).ToList();
            List<int> tokenIdList = filteredPlotsMegaHuge.GroupBy(x => x.token_id).Select(grp => grp.Key).ToList();

            // Remove all L6 and L7 plots that are not in the fullProcessTokenList of Token_id's : POSSIBLE PROBLEM - plots related to old token (dropped after upgrade) may not get processed.
            plotList.RemoveAll(x => (x.building_level == 7 || x.building_level == 6) && !fullProcessTokenIdList.Contains(x.token_id));

            // Add one plot for each L6 and L7 back into FULL PROCESS Plot list.  Dont add dups hence check against fullProcessTokenIdList.
            for (int counter = 0; counter < tokenIdList.Count; counter++)
            {
                if (!fullProcessTokenIdList.Contains(tokenIdList[counter]))
                {
                    plotList.Add(filteredPlotsMegaHuge.Where(x => x.token_id == tokenIdList[counter]).First());
                }
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

        // 3 SYS Accounts used to store parcel and custom buildings - these accounts should be ignored and full sync applied
        // BNB : 0xb352220bc15c37ce938349dd7d66959d832aa9d8
        // TRX : 0x1e011a567708673a220c6249fc1415b9f87f1cba
        // ETH : 0xe89eb9ffc12621d7c111c5b9d27be9a311917482
        private static bool CheckSysOwner(string accountMatic)
        {
            bool sysAccountIdentified = false;

            if (accountMatic.ToLower() == "0xb352220bc15c37ce938349dd7d66959d832aa9d8" || accountMatic.ToLower() == "0x1e011a567708673a220c6249fc1415b9f87f1cba" || accountMatic.ToLower() == "0xe89eb9ffc12621d7c111c5b9d27be9a311917482")
            {
                sysAccountIdentified = true;
            }

            return sysAccountIdentified;
        }


        // Remove all empty owned plots && built plots with unchanged IP
        // NOTE: Difference between count (plots removed) VS count (plots updated) is due to (a) [plots updated] is buildings and Empty plot count with 1 or more plots per building and (b) plot removed is count of individual plots
        // 2023/08/13 Enh : fullProcessTokenIdList - list of tokenId's of demolished buildings, each of these former building plots needs to FULL process sync run.
        // FUTURE PERF ENH: as of 2023/08  ANY POI change within a district OR ANY monument change for an player account CAUSES 
        public int RemoveAccountPlot(int waitPeriodMS, List<OwnerChange> ownerChangeList, List<DistrictName> districtListMCPBasic, List<Plot> plotList, List<int> fullProcessTokenIdList)
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
            bool refreshMission = true;

            int buildingTypeId = 0, districtId = 0, tokenId = 0, buildingLevel = 0, storedInfluenceBonus = 0, influenceBonus = 0, storedInfluence = 0, influence = 0, staminaAlertCount =0 , citizenAssignedCount = 0;
            int buildingUpdatedCount = 0, emptyPlotsUpdatedCount = 0;
            int storedApp123bonus = 0, storedApp4 = 0, storedApp5 = 0, storedInfluenceInfo = 0;
            DateTime? last_action;
            bool lastActionChanged = false, influanceChange = false;

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

                // SKIP SYS owner account used to stored all Parcels and Custom Buildings - these need to be Full Processed.
                if (CheckSysOwner(accountWithMin2Plot[i]))
                {
                    continue;
                }

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
                            alert.AddLowStaminaAlert(accountWithMin2Plot[i], 1, false);
                        }
                    }

                    // Skip first account plot (start at 1 not 0) : full sync needed on that first account plot which will retrive latest avatar and name used by account.
                    for (int landIndex = 1; landIndex < lands.Count; landIndex++)
                    {

                        buildingTypeId = lands[landIndex].Value<int?>("building_type_id") ?? 0;
                        //parcelId = lands[landIndex].Value<int?>("building_type_id") ?? 0;
                        tokenId = lands[landIndex].Value<int?>("token_id") ?? 0;
                        buildingLevel = lands[landIndex].Value<int?>("building_level") ?? 0;                        
                        citizenAssignedCount = citizen.GetCitizenCount(lands[landIndex].Value<JArray>("citizens"));

                        buildingPlotList = _context.plot.Where(x => x.token_id == tokenId).ToList();
                        if (buildingPlotList.Count == 0 || buildingPlotList[0].owner_matic.ToLower() != accountWithMin2Plot[i])
                        {
                            continue;   // if newly minted plot with new token OR plot sold/transfer, then get/process full plot - not partial.
                        }

                        last_action = ServiceCommon.UnixTimeStampUTCToDateTime(lands[landIndex].Value<double?>("last_action"), null);
                        lastActionChanged = buildingPlotList[0].last_action != last_action;
                        

                        districtId = lands[landIndex].Value<int?>("region_id") ?? 0;
                        districtName = districtListMCPBasic.Where(x => x.district_id == districtId).FirstOrDefault();
                        districtPOIStateChanged = districtName != null && (districtName.poi_activated || districtName.poi_deactivated);

                        influenceBonus = lands[landIndex].Value<int?>("influence_bonus") ?? 0;
                        influence = lands[landIndex].Value<int?>("influence") ?? 0;
                        storedInfluenceBonus = buildingPlotList[0].influence_bonus ?? 0;
                        storedInfluence = buildingPlotList[0].influence ?? 0;
                        storedInfluenceInfo = buildingPlotList[0].influence_info ?? 0;
                        storedApp123bonus = buildingPlotList[0].app_123_bonus ?? 0;
                        storedApp4 = buildingPlotList[0].app_4_bonus ?? 0;
                        storedApp5 = buildingPlotList[0].app_5_bonus ?? 0;

                        // CHECK: if no Plot IP change(due to POI or Monument state change)
                        //  AND no IP bonus change - or anomoly found with stored_app_bonus components vs influenceBonus,
                        //  AND no change in IP due to nearby building (as influence_info would need to be recalculated)
                        influanceChange = influenceBonus != storedInfluenceBonus || influence != storedInfluence || influenceBonus != (storedApp123bonus + storedApp4 + storedApp5);


                        // NOTE: Empty plot needs to be updated on each nighly sync - as empty plot can be set For_sale or sale price changed.
                        // NOTE_2: Newly build POI and monuments wont trigger a state change until next sync, unless account manually viewed and plots updated before sync
                        // NOTE_3: The influence attributes check is needed as building may be destroyed reverting back to empty plot, plot influance fields will need full sync to all reflect current 0 value.

                        // IDENTIFY & REMOVE PLOTS FROM FULL PROCESS LIST 
                        // A) Check if building demolished in last 24 hours, then run full plot sync process on all building plots.
                        //      If current building level is less stored or type has changed then building was demolished (and maybe rebuild as different type)
                        if (buildingLevel < buildingPlotList[0].building_level || buildingTypeId != buildingPlotList[0].building_type_id)
                        {
                            fullProcessTokenIdList.Add(tokenId);
                        }
                        // B) REMOVE EMPTY LAND PLOTS - Checking & Saving "For Sale" via UpdatePlotPartial() Process.
                        else if ((buildingTypeId == (int)BUILDING_TYPE.EMPTY_LAND || buildingTypeId == (int)BUILDING_TYPE.POI)
                            && influence == 0 && influenceBonus == 0 && storedInfluenceInfo == 0)
                        {
                            UpdatePlotPartial(lands[landIndex], false, refreshMission);
                            emptyPlotsUpdatedCount++;

                            totalPlotsRemoved += plotList.RemoveAll(x =>
                               x.owner_matic == accountWithMin2Plot[i] &&
                               x.token_id == tokenId);
                        }
                        // C) SKIP INDUSTRY BUILDING - If an action change has been registered. Rule: Need to identify Production run type (action_id from Plot Full Process Sync)    
                        // CHECK: if no building IP changed due to (a) POI or Monument state change,  (b) MCP building influence differs to local stored influence        
                        //  THEN skip Full update and use Partial update.
                        else if (ownerMonumentStateChanged == false && districtPOIStateChanged == false && influanceChange == false &&
                            (buildingTypeId != (int)BUILDING_TYPE.INDUSTRIAL || (buildingTypeId == (int)BUILDING_TYPE.INDUSTRIAL && lastActionChanged == false)))
                        {
                            PlotCord fullUpdate = UpdatePlotPartial(lands[landIndex], false, refreshMission);
                            // Safety measure - UpdatePlotPartial may identify that a full update is preferred for this building.
                            if (fullUpdate != null && fullUpdate.fullUpdateRequired)
                            {
                                fullProcessTokenIdList.Add(tokenId);
                            }
                            else
                            {
                                buildingUpdatedCount++;
                            }

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
                        alert.AddLowStaminaAlert(accountWithMin2Plot[i], staminaAlertCount, false);
                    }

                    // Save after each user account processed.
                    try
                    {
                        _context.SaveWithRetry();
                    }
                    catch (Exception ex)
                    {
                        DBLogger dBLogger = new(_context.worldTypeSelected);
                        dBLogger.logException(ex, String.Concat("PlotManage:RemoveEmptyPlot() :   Error processing plot - failed on plot token_id : ", tokenId));
                    }
                }

            }

            _context.LogEvent(String.Concat("PlotManage:RemoveEmptyPlot() :  Empty Plots updated : ", emptyPlotsUpdatedCount, ",  Buildings updated : ", buildingUpdatedCount));            

            return totalPlotsRemoved;
        }

        // Lazy update of plots using Full plot update 
        // Used to improve accuracy of ranking feature, where user loads portfolio - identifing plot.influence change >> then needs full update for Ranking
        public async Task<int> UpdateBuildingAsyncFull(List<PlotCord> tokenIdList)
        {
            // Generate a new dbContext as a safety measure - insuring log is recorded.  Service trigged has already ended.
            using (var _contextJob = new MetaverseMaxDbContext(worldType))
            {
                PlotManage plotManage = new PlotManage(_contextJob, worldType);

                foreach (PlotCord plotCord in tokenIdList)
                {
                    plotManage.AddOrUpdatePlot(plotCord.plotId, plotCord.posX, plotCord.posY, true);        // MUST save to db when making these changes, due to 1 second interval, and impact on ranking league - stale data.
                    await Task.Delay(1000);
                }
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

            emptyPlotFilterCount = RemoveAccountPlot(jobInterval, ownerChangeList, districtListMCPBasic, plotList, new List<int>());     // removes all account plots from process list matching criteria.

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

        public WorldMissionWeb GetMissionActive()
        {
            MissionDB missionDB;
            OwnerManage ownerManage;
            IEnumerable<MissionActive> missionActive = null;
            WorldMissionWeb worldMission = new();
            ServiceCommon common = new();
            Building building = new();

            try
            {
                BuildingManage buildingManage = new(_context, worldType);
                missionDB = new(_context, worldType);
                ownerManage = new(_context, worldType);
                missionActive = missionDB.MissionActiveGet();

                worldMission.mission_list = missionActive.Select(mission => {

                    OwnerAccount ownerAccount = ownerManage.GetOwnerAccountByMatic(mission.owner_matic);
                    ProductionCollection productionCollection = null;

                    var buildingType = (BUILDING_TYPE)mission.building_type_id;
                    var buildingLevel = mission.building_level;
                    var lastActionUx = ((DateTimeOffset)(mission.last_action)).ToUnixTimeSeconds();

                    // Find next collection details on active building
                    productionCollection = buildingManage.CollectionEval(buildingType, buildingLevel, lastActionUx);                    

                    return new MissionWeb
                    {
                        token_id = mission.token_id,
                        pos_x = mission.pos_x,
                        pos_y = mission.pos_y,
                        district_id = mission.district_id,                 
                        owner_matic = mission.owner_matic,
                        owner_name = ownerAccount == null ? "" : ownerAccount.name.IsNullOrEmpty() ? ownerAccount.discord_name ?? string.Empty : String.Empty,
                        owner_avatar_id = ownerAccount == null ? 0 : ownerAccount.avatar_id,
                        building_id = mission.building_id,
                        building_level = mission.building_level,
                        building_type_id  = mission.building_type_id,
                        building_img = building.GetBuildingImg((BUILDING_TYPE)mission.building_type_id, mission.building_id, mission.building_level, worldType),

                        last_refresh = (int)TimeSpan.FromTicks(DateTime.UtcNow.Ticks - mission.last_updated.Ticks).TotalMinutes,
                        last_updatedUx = ((DateTimeOffset)(mission.last_updated)).ToUnixTimeSeconds(),
                        last_updated = common.LocalTimeFormatStandardFromUTC(string.Empty, mission.last_updated),

                        completed = mission.completed,
                        max = mission.max,
                        reward = mission.reward,
                        reward_owner = mission.reward_owner,
                        available = mission.available,
                        balance = mission.balance,

                        c_r = productionCollection == null ? false : productionCollection.ready,
                        c_d = productionCollection == null ? 0 : productionCollection.day,
                        c_h = productionCollection == null ? 0 : productionCollection.hour,
                    };
                }).OrderByDescending(x => x.reward);

                worldMission.mission_count = worldMission.mission_list.Count();
                worldMission.mission_reward = Math.Round(worldMission.mission_list.Sum(x=>x.reward), 2);

                worldMission.all_mission_count = worldMission.mission_list.Sum(x => x.max);
                worldMission.all_mission_available_count = worldMission.all_mission_count - worldMission.mission_list.Sum(x => x.completed);
                worldMission.all_mission_reward = Math.Round(worldMission.mission_list.Sum(x => x.reward * x.max), 0);
                worldMission.all_mission_available_reward = Math.Round(worldMission.mission_list.Sum(x => x.reward * (x.max - x.completed)), 0);
                worldMission.repeatable_daily_reward = Math.Round(worldMission.mission_list.Where(x => x.building_level == 7).Sum( x=> x.reward), 2);
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotManage::GetMissionActive() : Error getting missions "));
            }

            return worldMission;

        }

        public WorldParcelWeb GetParcel(int districtId)
        {
            BuildingParcelDB buildingParcelDB;
            List<BuildingParcel> parcelList = new();
            WorldParcelWeb worldParcel = new WorldParcelWeb();
            OwnerManage ownerManage;            
            ServiceCommon common = new();
            Building building = new();

            try
            {
                buildingParcelDB = new(_context);
                ownerManage = new(_context, worldType);
                parcelList = buildingParcelDB.ParcelGet(districtId);

                worldParcel.parcel_list = parcelList.Select(land => {

                    OwnerAccount ownerAccount = ownerManage.GetOwnerAccountByMatic(land.owner_matic);

                    return new ParcelWeb
                    {
                        parcel_id = land.parcel_id,
                        pos_x = land.pos_x,
                        pos_y = land.pos_y,
                        district_id = land.district_id,
                        building_img = building.GetBuildingImg(BUILDING_TYPE.PARCEL, 0, 0, worldType, land.parcel_info_id ?? 0, land.parcel_id),
                        building_name = land.building_name,
                        unit_count = land.parcel_unit_count ?? 0,
                        owner_matic = land.owner_matic,
                        owner_name = ownerAccount == null ? "" : ownerAccount.name,
                        owner_avatar_id = ownerAccount == null ? 0 : ownerAccount.avatar_id,
                        forsale = land.on_sale,
                        forsale_price = land.current_price,
                        last_actionUx = ((DateTimeOffset)(land.last_action ?? land.last_updated)).ToUnixTimeSeconds(),
                        last_action = common.LocalTimeFormatStandardFromUTC(string.Empty, land.last_action ?? land.last_updated),
                        action_type = land.last_action_type,
                        plot_count = land.plot_count,
                        building_category_id = land.building_category_id ?? 0,
                        floor_count = land.floor_count ?? 0,
                        unit_forsale_count = land.unit_forsale_count ?? 0,
                        unit_price_high_coin = land.unit_price_high_coin ?? 0,
                        unit_price_low_coin = land.unit_price_low_coin ?? 0,
                        unit_price_high_mega = land.unit_price_high_mega ?? 0,
                        unit_price_low_mega = land.unit_price_low_mega ?? 0,
                        unit_sale_largest_size  = land.unit_sale_largest_size ?? 0,
                        unit_sale_smallest_size = land.unit_sale_smallest_size ?? 0
                    };
                }).OrderByDescending(x => x.plot_count);

                worldParcel.parcel_count = worldParcel.parcel_list.Count(x => x.building_category_id == 0);
                worldParcel.building_count = worldParcel.parcel_list.Count() - worldParcel.parcel_count;

            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotManage::GetParcel() : Error occured using district_id : ", districtId));
            }

            return worldParcel;
        }        

        private async Task<JObject> GetSaleInfo(int tokenId, int tokenType)
        {
            string content = string.Empty;
            JObject jsonContent = null;

            try
            {
               serviceUrl = worldType switch { WORLD_TYPE.TRON => TRON_WS.SALES_INFO, WORLD_TYPE.BNB => BNB_WS.SALES_INFO, WORLD_TYPE.ETH => ETH_WS.SALES_INFO, _ => TRON_WS.SALES_INFO };

                // POST REST WS
                HttpResponseMessage response;
                using (var client = new HttpClient(getSocketHandler()) { Timeout = new TimeSpan(0, 0, 60) })
                {
                    StringContent stringContent = new StringContent("{\"token_id\": \"" + tokenId + "\", \"token_type\": \"" + tokenType + "\" }", Encoding.UTF8, "application/json");

                    response = await client.PostAsync(
                        serviceUrl,
                        stringContent);

                    response.EnsureSuccessStatusCode(); // throws if not 200-299
                    content = await response.Content.ReadAsStringAsync();
                }

                watch.Stop();
                servicePerfDB.AddServiceEntry(serviceUrl, serviceStartTime, watch.ElapsedMilliseconds, content.Length, tokenId.ToString());

                if (content.Length > 0)
                {
                    jsonContent = JObject.Parse(content);                    
                }
                        
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotManage.GetSaleInfo() : Error on WS calls for token_id : ", tokenId));
            }

            return jsonContent;
        }

        // Special Case : Partial update during day - if building was recently built/upgraded, partial update of plot data found in /user/assets/lands WS calls.
        // Assets/land WS does NOT return these fields:
        //      owner_nickname
        //      owner_avatar_id
        //      resources
        //      influence_info
        //      influence_poi_bonus
        //      production_poi_bonus
        //      is_perk_activated
        //      app_4_bonus
        //      app_5_bonus
        //      app_123_bonus
        //      action_id     >>> Industry buildings (type=5),  action_id indicates what the current production type, ONLY INDUSTRY building can run 2 different production run types - USED BY Ranking prediction eval.
        //      for_rent      >>> bool flag is not provided, BUT can infer if plot is currently rented by renter (matic) field and if not currently rented - if a rent price is assigned (0 =  not available for rent)
        //
        //  Custom Buildings >> parcel_id attribte is not included within Owner>>Lands WS MCP call, as such Custom Building is not identified or updated here (only within FULL Plot sync calls)
        //  Building Mission >> Find and Update Bilding Mission is Optional (Nightly Sync checks for missions, MyPortfolio loading does not for optimal perf)
        //  
        //  Full Plot Update Identifier Logic:
        //      If IP or Last_Action date differs from recorded in local db, mark plot for full sync - due to missing changes.
        public PlotCord UpdatePlotPartial(JToken ownerLand, bool saveEvent, bool refreshMission)
        {
            ServiceCommon common = new();
            Plot plotMatched = null;
            List<Plot> buildingPlotList = null;
            CitizenManage citizen = new(_context, worldType);
            BuildingManage buildingManage = new(_context, worldType);
            Building building = new();
            int tokenId = 0, buildingLevel = 0, oldTokenId = 0;
            int newInfluence = 0;
            Boolean fullPlotSync = false;
            DateTime? last_action;
            decimal newRanking = -1;
            PlotCord plotFullUpdate = new() { fullUpdateRequired = false };
            bool hasMission = false, newMission = false;
            decimal balance = 0;

            try
            {
                MissionDB missionDB = new(_context, worldType);
                oldTokenId = tokenId = ownerLand.Value<int?>("token_id") ?? 0;
                buildingPlotList = _context.plot.Where(x => x.token_id == tokenId).ToList();

                // CHECK local db shows unclaimed.
                if (buildingPlotList.Count == 0)
                {
                    // Newly claimed plot in last 24 hrs - get(WS land/get) full plot details and update db.
                    plotMatched = AddOrUpdatePlot(0, ownerLand.Value<int?>("x") ?? 0, ownerLand.Value<int?>("y") ?? 0, saveEvent);                    
                    _context.LogEvent(String.Concat("New plot token_id found - Get full plot details and store : X: ", ownerLand.Value<int?>("x") ?? 0, "  Y:", ownerLand.Value<int?>("y") ?? 0));
                }
                else
                {
                    plotMatched = buildingPlotList[0];
                    buildingLevel = ownerLand.Value<int?>("building_level") ?? 0;

                    // CHECK if owner changed == plot was transfered or sold since last sync, as owner_nickname and owner_avatar_id are not provided with this service call - need to get them
                    if (plotMatched.owner_matic.ToLower() != ownerLand.Value<string>("owner").ToLower())
                    {
                        // Newly claimed/transfer/sold plot in last 24 hrs - get(WS land/get) full plot details and update db.
                        // NOTE: This call will also update all related plots in building (MEGA / HUGE)
                        plotMatched = AddOrUpdatePlot(plotMatched.plot_id, ownerLand.Value<int?>("x") ?? 0, ownerLand.Value<int?>("y") ?? 0, saveEvent);
                        _context.LogEvent(String.Concat("Plot sold/transfer - Get full plot details and store - due to change of owner_matic, avatar, name.  Plot tokenID - ", tokenId));

                    }
                    else if (buildingPlotList.Count == 1 && buildingLevel < (int)BUILDING_SIZE.HUGE)
                    {
                        // On Influence change - tag token for full update.
                        // MCP BUG - calulated total IP with any POI & Monument included and ALWAYS active[using this LANDS WS] 
                        newInfluence = ownerLand.Value<int?>("influence") ?? 0;
                        fullPlotSync = plotMatched.influence != newInfluence || fullPlotSync == true;

                        last_action = ServiceCommon.UnixTimeStampUTCToDateTime(ownerLand.Value<double?>("last_action"), null);
                        //fullPlotSync = plotMatched.last_action != last_action || fullPlotSync == true;
                        plotMatched.last_action = last_action;

                        // Update 1 plot or multiple plots depending on building level, only one record returned per building with this lands WS call.
                        // Notes: for_rent is not populated by this WS call, unknown if rental data is valid.  Dont change last_updated as not all fields are updated - specifically the influance_info field is not updated which used in ranking module to allow another refresh.
                        plotMatched.update_type = (int)UPDATE_TYPE.PARTIAL;
                        plotMatched.last_updated = DateTime.UtcNow;
                        plotMatched.unclaimed_plot = string.IsNullOrEmpty(ownerLand.Value<string>("owner"));
                        plotMatched.owner_matic = ownerLand.Value<string>("owner");

                        plotMatched.building_id = ownerLand.Value<int?>("building_id") ?? 0;
                        plotMatched.building_level = buildingLevel;
                        plotMatched.building_type_id = ownerLand.Value<int?>("building_type_id") ?? 0;
                        plotMatched.token_id = tokenId;
                        plotMatched.on_sale = ownerLand.Value<bool?>("on_sale") ?? false;
                        plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType) : 0;
                        plotMatched.rented = ownerLand.Value<string>("renter") != null;
                        plotMatched.for_rent = plotMatched.rented == false ? building.GetRentPrice(ownerLand.Value<JToken>("rent_info"), worldType) : 0;    // Only get rent price if not currently rented
                        plotMatched.abundance = ownerLand.Value<int?>("abundance") ?? 0;
                        plotMatched.condition = ownerLand.Value<int?>("condition") ?? 0;

                        plotMatched.current_influence_rank = buildingManage.CheckInfluenceRankChange(newInfluence, plotMatched.influence ?? 0, plotMatched.influence_bonus ?? 0, plotMatched.current_influence_rank ?? 0, buildingLevel, plotMatched.building_type_id, plotMatched.token_id, plotMatched.building_id, plotMatched.district_id, plotMatched.owner_matic);
                        plotMatched.influence = newInfluence;                                                   // Placed after ranking check, as both old and new influence needed for check                    

                        //plotMatched.influence_bonus = ownerLand.Value<int?>("influence_bonus") ?? 0;          // Missing assign bonus per app slot

                        plotMatched.citizen_count = ownerLand.Value<JArray>("citizens") == null ? 0 : ownerLand.Value<JArray>("citizens").Count;
                        plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(ownerLand.Value<JArray>("citizens"), plotMatched.building_type_id);

                    }
                    else if ((ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.HUGE && buildingPlotList.Count != 2)|| (ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.MEGA && buildingPlotList.Count != 4))
                    {
                        // Recent upgraded huge or mega, complete full refresh on plot. local bulding amount of plots has changed versus current building level.
                        // NOTE : On Huge to Mega upgrade, will only be able to update 2 plots out of 4, as unknown from data we have as to which other huge used in upgrade. TO_DO find a smart way...  It will get picked up on Nighly sync
                        fullPlotSync = true;
                        oldTokenId = plotMatched.token_id;

                    }
                    else if ((ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.HUGE || ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.MEGA) || buildingPlotList.Count > 1)
                    {
                        // On Influence change - tag token for full update.
                        newInfluence = ownerLand.Value<int?>("influence") ?? 0;
                        fullPlotSync = plotMatched.influence != newInfluence || fullPlotSync == true;

                        last_action = ServiceCommon.UnixTimeStampUTCToDateTime(ownerLand.Value<double?>("last_action") ?? 0, null);
                        //fullPlotSync = plotMatched.last_action != last_action || fullPlotSync == true;

                        for (int i = 0; i < buildingPlotList.Count; i++)
                        {
                            plotMatched = buildingPlotList[i];

                            plotMatched.last_action = last_action;
                            plotMatched.update_type = (int)UPDATE_TYPE.PARTIAL;
                            plotMatched.last_updated = DateTime.UtcNow;
                            plotMatched.unclaimed_plot = string.IsNullOrEmpty(ownerLand.Value<string>("owner"));
                            plotMatched.owner_matic = ownerLand.Value<string>("owner");

                            plotMatched.building_id = ownerLand.Value<int?>("building_id") ?? 0;
                            plotMatched.building_level = buildingLevel;
                            plotMatched.building_type_id = ownerLand.Value<int?>("building_type_id") ?? 0;
                            plotMatched.token_id = ownerLand.Value<int?>("token_id") ?? 0;
                            plotMatched.on_sale = ownerLand.Value<bool?>("on_sale") ?? false;
                            plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType) : 0;
                            plotMatched.rented = ownerLand.Value<string>("renter") != null;
                            plotMatched.for_rent = plotMatched.rented == false ? building.GetRentPrice(ownerLand.Value<JToken>("rent_info"), worldType) : 0;
                            //plotMatched.abundance = ownerLand.Value<int?>("abundance") ?? 0;                  // dont update abundance for related building plots - each plot can have own abundance
                            plotMatched.condition = ownerLand.Value<int?>("condition") ?? 0;

                            // Only apply ranking calc on first plot of multiplot building
                            if (newRanking == -1)
                            {
                                newRanking = buildingManage.CheckInfluenceRankChange(newInfluence, plotMatched.influence ?? 0, plotMatched.influence_bonus ?? 0, plotMatched.current_influence_rank ?? 0, buildingLevel, plotMatched.building_type_id, plotMatched.token_id, plotMatched.building_id, plotMatched.district_id, plotMatched.owner_matic);
                            }
                            plotMatched.current_influence_rank = newRanking;                                    // Reuse ranking if already identified on prior related plot
                            plotMatched.influence = newInfluence;                                               // Placed after ranking check, as both old and new influence needed for check

                            //plotMatched.influence_bonus = ownerLand.Value<int?>("influence_bonus") ?? 0;      // MCP calulated total IP with any POI & Monument included and ALWAYS active

                            plotMatched.citizen_count = ownerLand.Value<JArray>("citizens") == null ? 0 : ownerLand.Value<JArray>("citizens").Count;
                            plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(ownerLand.Value<JArray>("citizens"), plotMatched.building_type_id);
                        }                        
                    }

                    balance = building.convertPriceMega(ownerLand.Value<decimal?>("balance") ?? 0);
                    hasMission = ownerLand.Value<bool?>("has_mission") ?? false;
                }

                if (hasMission && balance > 0)
                {                    
                    newMission = missionDB.CheckHasMission(tokenId) == false;                                               
                }

                // Store plot data for later full update (if enabled)
                // Identify Plot/Building that requires full Update Sync - due to changes not found in a partial update (using Lands WS)
                if (fullPlotSync)
                {
                    plotFullUpdate.fullUpdateRequired = true;
                    plotFullUpdate.plotId = plotMatched.plot_id;
                    plotFullUpdate.posX = ownerLand.Value<int?>("x") ?? 0;
                    plotFullUpdate.posY = ownerLand.Value<int?>("y") ?? 0;
                    plotFullUpdate.localTokenID = oldTokenId;
                    plotFullUpdate.currentTokenID = tokenId;
                }

                // Refresh mission requires MCP WS call,  always pull in new missions - dont refresh existing active missions unless flagged to do so.
                if ((hasMission && refreshMission) || newMission)
                {                    
                    JObject buildingMission = Task.Run(() => GetBuildingMissionMCP(tokenId)).Result;                    
                    missionDB.AddOrUpdate(buildingMission, tokenId, balance);
                }

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                DBLogger dBLogger = new(_context.worldTypeSelected);
                dBLogger.logException(ex, String.Concat("PlotDB:UpdatePlotPartial() : Error Adding/update Plot token_id: ", tokenId));
            }

            return plotFullUpdate;
        }
    }

}
