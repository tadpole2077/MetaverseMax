using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public partial class PlotDB : DatabaseBase
    {
        public PlotDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;          
        }

        public PlotDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {         
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldTypeSelected;
        }

        public DbSet<Plot> Plots { get; set; }   // Links to a specific table in DB

        public List<Plot> GetPlotbyToken(int tokenId)
        {
            List<Plot> plotList = null;
            try 
            {
                plotList = _context.plot.Where(x => x.token_id == tokenId).ToList();

                for (int index = 1; index < plotList.Count; index++)
                {
                    plotList[index].influence_bonus ??= 0;
                    plotList[index].influence ??= 0;
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB:GetPlotbyToken() : Error getting Plot with token_id:", tokenId));
            }

            return plotList;
        }

        public Plot GetPlotbyPosXPosY(int posX, int posY)
        {
            Plot plot = null;
            try 
            {
                plot = _context.plot.Where(x => x.pos_x == posX && x.pos_y == posY).FirstOrDefault();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB:GetPlotbyPosXPosY() : Error getting Plot with posX =", posX, " posY=", posY));
            }

            return plot;
        }

        public IEnumerable<Plot> PlotsGet_ByOwnerMatic(string maticKey)
        {
            List<Plot> plotList;

            plotList = _context.plot.Where(x => x.owner_matic == maticKey).OrderBy(x => x.plot_id).ToList();

            return plotList.ToArray();
        }

        public Plot GetLastPlotUpdated()
        {
            Plot foundPlot = new();
            try
            {
                // Select type query using LINQ returning a collection of row matching condition - selecting first row.               
                foundPlot = _context.plot.OrderByDescending(x => x.last_updated).FirstOrDefault();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PlotDB:GetLastPlotUpdated() : Error Getting last updated Plat"));
                _context.LogEvent(log);
            }

            return foundPlot == null ? new Plot() : foundPlot;
        }

        public int ArchivePlots()
        {
            int result = 0;
            try
            {
                //exec sproc - create a dup set of plots within Archive table if not previously archived.
                result = _context.Database.ExecuteSqlRaw("EXEC dbo.sp_archive_plots");

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PlotDB::ArchivePlots() : Error Archiving Plots using sproc sp_archive_plots "));
                _context.LogEvent(log);
            }

            return result;
        }

        public List<PlotIP> GetIP_Historic(int tokenId)
        {
            List<PlotIP> plotIPList = new();

            try
            {
                plotIPList = _context.plotIP.FromSqlInterpolated($"EXEC dbo.sp_plot_IP_get {tokenId}").AsNoTracking().ToList();

                for(int index=0; index < plotIPList.Count; index++)
                {
                    plotIPList[index].total_ip = (int)Math.Round(
                        (decimal)(plotIPList[index].influence_info ?? 0) * (1 + ((plotIPList[index].influence_bonus ?? 0) / 100m)), 
                        0, 
                        MidpointRounding.AwayFromZero);

                    //(decimal)(1 + (((plotIPList[index].app_123_bonus ?? 0) +
                    //    ((plotIPList[index].is_perk_activated ?? false) ? (plotIPList[index].app_4_bonus ?? 0) + (plotIPList[index].app_5_bonus ?? 0) : 0) )

                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PlotDB::GetIP_Historic() : Error getting a single Plot's historic IP from Archive using token_id : ", tokenId));
                _context.LogEvent(log);
            }

            return plotIPList;
        }

        public RETURN_CODE UpdatePlot(int tokenId, decimal influenceEfficiency, int predictProduce, int lastRunProduce, int produceId, DateTime? lastRunProduceDate, bool lastRunProducePredict, int buildingAbundance, bool saveEvent)
        {
            List<Plot> plotList;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            try
            {
                plotList = GetPlotbyToken(tokenId);

                for (int index = 0; index < plotList.Count; index++)
                {
                    plotList[index].current_influence_rank = influenceEfficiency;
                    plotList[index].predict_produce = predictProduce;
                    plotList[index].last_run_produce_id = produceId;
                    plotList[index].last_run_produce_date = lastRunProduceDate;
                    plotList[index].last_run_produce = lastRunProduce;
                    plotList[index].last_run_produce_predict = lastRunProducePredict;
                    plotList[index].building_abundance = buildingAbundance == -1 ? plotList[index].building_abundance : buildingAbundance;
                }
                returnCode = RETURN_CODE.SUCCESS;

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::UpdatePlot() : Error updating Plot using token_id : ", tokenId));
            }

            return returnCode;
        }

        public RETURN_CODE UpdateRelatedBuildingPlot(int plotId)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int result;

            try
            {
                result = _context.Database.ExecuteSqlInterpolated($"EXEC dbo.sp_plot_update_building {plotId}");

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::UpdateRelatedBuildingPlot() : Error updating related building Plots using target plot_id : ", plotId));
            }

            return returnCode;
        }

        public Plot AddOrUpdatePlot(JObject jsonContent, int posX, int posY, int plotId, bool saveEvent)
        {
            Plot plotMatched = null, returnPlot = null;
            CitizenManage citizen = new(_context, worldType);
            Building building = new();

            try
            {
                // Only process if MCP service returned plot obj
                if (jsonContent != null)
                {
                    // Based on the callers passed plotId, either add a new plot or update an existing plot record.
                    if (plotId == 0)
                    {
                        // Defensive check Plot may already exist - error occured during initial world sync - plot dropped.
                        plotMatched = GetPlotbyPosXPosY(posX, posY);

                        if (plotMatched == null)
                        {
                            returnPlot = _context.plot.Add(new Plot()
                            {
                                pos_x = posX,
                                pos_y = posY,
                                cell_id = jsonContent.Value<int?>("cell_id") ?? 0,
                                district_id = jsonContent.Value<int?>("region_id") ?? 0,
                                land_type = jsonContent.Value<int?>("land_type") ?? 0,

                                last_updated = DateTime.UtcNow,                                
                                unclaimed_plot = string.IsNullOrEmpty(jsonContent.Value<string>("owner")),

                                owner_nickname = jsonContent.Value<string>("owner_nickname"),
                                owner_matic = jsonContent.Value<string>("owner"),
                                owner_avatar_id = jsonContent.Value<int>("owner_avatar_id"),

                                resources = jsonContent.Value<int?>("resources") ?? 0,
                                building_id = jsonContent.Value<int?>("building_id") ?? 0,
                                building_level = jsonContent.Value<int?>("building_level") ?? 0,
                                building_type_id = jsonContent.Value<int?>("building_type_id") ?? 0,
                                token_id = jsonContent.Value<int?>("token_id") ?? 0,

                                on_sale = jsonContent.Value<bool?>("on_sale") ?? false,
                                current_price = jsonContent.Value<bool?>("on_sale") ?? false ? building.GetSalePrice(jsonContent.Value<JToken>("sale_data"), worldType) : 0,

                                for_rent = (jsonContent.Value<int?>("for_rent") ?? 0) > 0 ? building.GetRentPrice(jsonContent.Value<JToken>("rent_info"), worldType) : 0,
                                rented = jsonContent.Value<string>("renter") != null,
                                abundance = jsonContent.Value<int?>("abundance") ?? 0,
                                building_abundance = 0,
                                condition = jsonContent.Value<int?>("condition") ?? 0,

                                influence_info = GetInfluenceInfoTotal(jsonContent.Value<JToken>("influence_info"), jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false, posX, posY, jsonContent.Value<int?>("building_type_id") ?? 0),
                                current_influence_rank = 0,
                                influence = jsonContent.Value<int?>("influence") ?? 0,
                                influence_bonus = jsonContent.Value<int?>("influence_bonus") ?? 0,
                                influence_poi_bonus = jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false,
                                production_poi_bonus = jsonContent.Value<decimal?>("production_poi_bonus") ?? 0.0m,
                                is_perk_activated = jsonContent.Value<Boolean?>("is_perk_activated") ?? false,
                                app_4_bonus = GetApplicationBonus(4, jsonContent.Value<JArray>("extra_appliances"), posX, posY),
                                app_5_bonus = GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY),
                                app_123_bonus = GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY),

                                low_stamina_alert = citizen.CheckCitizenStamina(jsonContent.Value<JArray>("citizens"), jsonContent.Value<int?>("building_type_id") ?? 0),
                                action_id = jsonContent.Value<int?>("action_id") ?? 0,

                                predict_produce = 0,
                                last_run_produce_id = 0,
                                last_run_produce_predict = false
                            }).Entity;
                        }
                        else
                        {
                            _context.LogEvent(String.Concat("PlotDB:AddOrUpdatePlot() : Existing plot was found for X:", posX, " Y:", posY,". This maybe unexpected! Call was to create new plot at these XY coord. Existing Plot will be updated"));
                        }
                    }
                    
                    if (plotId != 0 || plotMatched != null)
                    {
                        plotMatched = plotMatched == null ? _context.plot.Find(plotId) : plotMatched;

                        plotMatched.last_updated = DateTime.UtcNow;

                        plotMatched.unclaimed_plot = string.IsNullOrEmpty(jsonContent.Value<string>("owner"));
                        plotMatched.owner_nickname = jsonContent.Value<string>("owner_nickname");
                        plotMatched.owner_matic = jsonContent.Value<string>("owner");
                        plotMatched.owner_avatar_id = jsonContent.Value<int>("owner_avatar_id");
                        plotMatched.resources = jsonContent.Value<int?>("resources") ?? 0;
                        plotMatched.building_id = jsonContent.Value<int?>("building_id") ?? 0;
                        plotMatched.building_level = jsonContent.Value<int?>("building_level") ?? 0;
                        plotMatched.building_type_id = jsonContent.Value<int?>("building_type_id") ?? 0;
                        plotMatched.token_id = jsonContent.Value<int?>("token_id") ?? 0;

                        plotMatched.on_sale = jsonContent.Value<bool?>("on_sale") ?? false;
                        plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(jsonContent.Value<JToken>("sale_data"), worldType) : 0;
                        
                        plotMatched.for_rent = (jsonContent.Value<int?>("for_rent") ?? 0) > 0 ? building.GetRentPrice(jsonContent.Value<JToken>("rent_info"), worldType) : 0;
                        plotMatched.rented = jsonContent.Value<string>("renter") != null;
                        plotMatched.abundance = jsonContent.Value<int?>("abundance") ?? 0;
                        plotMatched.building_abundance = plotMatched.building_abundance;        // only update from BuildingManage class calc
                        plotMatched.condition = jsonContent.Value<int?>("condition") ?? 0;

                        plotMatched.influence_info = GetInfluenceInfoTotal(jsonContent.Value<JToken>("influence_info"), jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false, posX, posY, jsonContent.Value<int?>("building_type_id") ?? 0);
                        plotMatched.influence = jsonContent.Value<int?>("influence") ?? 0;
                        plotMatched.influence_bonus = jsonContent.Value<int?>("influence_bonus") ?? 0;
                        plotMatched.influence_poi_bonus = jsonContent.Value<Boolean?>("influence_poi_bonus") ?? false;
                        plotMatched.production_poi_bonus = jsonContent.Value<decimal?>("production_poi_bonus") ?? 0.0m;
                        plotMatched.is_perk_activated = jsonContent.Value<Boolean?>("is_perk_activated") ?? false;
                        plotMatched.app_4_bonus = GetApplicationBonus(4, jsonContent.Value<JArray>("extra_appliances"), posX, posY);
                        plotMatched.app_5_bonus = GetApplicationBonus(5, jsonContent.Value<JArray>("extra_appliances"), posX, posY);
                        plotMatched.app_123_bonus = GetApplication123Bonus(jsonContent.Value<JToken>("appliances"), posX, posY);

                        plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(jsonContent.Value<JArray>("citizens"), plotMatched.building_type_id);
                        plotMatched.action_id = jsonContent.Value<int?>("action_id") ?? 0;

                        plotMatched.predict_produce = plotMatched.predict_produce == null ? 0 : plotMatched.predict_produce;

                        returnPlot = plotMatched;
                    }
                }

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB:AddOrUpdatePlot() : Error Adding/update Plot X:", posX, " Y:", posY));
            }

            return returnPlot;
        }


        // Special Case : Partial update during day - if building was recently built/upgraded, partial update of plot data found in /user/assets/lands WS calls.
        //  assets/land WS does not return these fields:
        //      owner_nickname
        //      owner_avatar_id
        //      resources
        //      influence_info
        //      influence_bonus
        //      influence_poi_bonus
        //      production_poi_bonus
        //      is_perk_activated
        //      app_4_bonus
        //      app_5_bonus
        //      app_123_bonus
        //      action_id
        //      for_rent
        public bool UpdatePlotPartial(JToken ownerLand, bool saveEvent)
        {
            Plot plotMatched = null;
            List<Plot> buildingPlotList = null;
            PlotManage plotManage = new(_context, worldType);
            CitizenManage citizen = new(_context, worldType);
            Building building = new();
            int tokenId = 0;

            try
            {
                                                   
                tokenId = ownerLand.Value<int?>("token_id") ?? 0;
                buildingPlotList = _context.plot.Where(x => x.token_id == tokenId).ToList();

                if (buildingPlotList.Count == 0)
                {
                    // Newly claimed plot in last 24 hrs - get(WS land/get) full plot details and update db.
                    plotManage.AddOrUpdatePlot(0, ownerLand.Value<int?>("x") ?? 0, ownerLand.Value<int?>("y") ?? 0, saveEvent);
                    logInfo(String.Concat("New plot token_id found - Getting full plot details and storing : tokenID - ", tokenId));
                }
                else
                {
                    plotMatched = buildingPlotList[0];

                    // CHECK if owner changed == plot was transfered or sold since last sync, as owner_nickname and owner_avatar_id are not provided with thsi service call - need to get them
                    if (plotMatched.owner_matic.ToLower() != ownerLand.Value<string>("owner").ToLower())
                    {
                        // Newly claimed/transfer/sold plot in last 24 hrs - get(WS land/get) full plot details and update db.
                        // NOTE: This call will also update all related plots in building (MEGA / HUGE)
                        plotManage.AddOrUpdatePlot(0, ownerLand.Value<int?>("x") ?? 0, ownerLand.Value<int?>("y") ?? 0, true);
                        logInfo(String.Concat("Plot sold/transfered - Getting full plot details and storing - due to missing avatar & name: tokenID - ", tokenId));                        
                    }

                    else if (buildingPlotList.Count == 1 && ownerLand.Value<int?>("building_level") < (int)BUILDING_SIZE.HUGE)
                    {
                        // Update 1 plot or multiple plots depending on building level, only one record returned per building with this lands WS call.
                        // Notes: for_rent is not populated by this WS call, unknown if rental data is valid.  Dont change last_updated as not all fields are updated - specifically the influance_info field is not updated which used in ranking module to allow another refresh.

                        plotMatched.last_updated = DateTime.UtcNow;
                        plotMatched.current_price = building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType);
                        plotMatched.unclaimed_plot = string.IsNullOrEmpty(ownerLand.Value<string>("owner"));
                        //plotMatched.owner_nickname = jsonContent.Value<string>("owner_nickname");
                        plotMatched.owner_matic = ownerLand.Value<string>("owner");
                        //plotMatched.owner_avatar_id = jsonContent.Value<int>("owner_avatar_id");
                        //plotMatched.resources = jsonContent.Value<int?>("resources") ?? 0;
                        plotMatched.building_id = ownerLand.Value<int?>("building_id") ?? 0;
                        plotMatched.building_level = ownerLand.Value<int?>("building_level") ?? 0;
                        plotMatched.building_type_id = ownerLand.Value<int?>("building_type_id") ?? 0;
                        plotMatched.token_id = tokenId;
                        plotMatched.on_sale = ownerLand.Value<bool?>("on_sale") ?? false;
                        plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType) : 0;
                        plotMatched.rented = ownerLand.Value<string>("renter") != null;
                        plotMatched.for_rent = plotMatched.rented == false ? building.GetRentPrice(ownerLand.Value<JToken>("rent_info"), worldType) : 0;
                        plotMatched.abundance = ownerLand.Value<int?>("abundance") ?? 0;
                        plotMatched.condition = ownerLand.Value<int?>("condition") ?? 0;

                        plotMatched.influence = ownerLand.Value<int?>("influence") ?? 0;        // MCP calulated total IP with any POI & Monument included and ALWAYS active

                        plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(ownerLand.Value<JArray>("citizens"), plotMatched.building_type_id);
                    }
                    else if (ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.HUGE || ownerLand.Value<int?>("building_level") == (int)BUILDING_SIZE.MEGA)
                    {

                        for (int i = 0; i < buildingPlotList.Count; i++)
                        {
                            plotMatched = buildingPlotList[i];

                            plotMatched.last_updated = DateTime.UtcNow;
                            plotMatched.current_price = building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType);
                            plotMatched.unclaimed_plot = string.IsNullOrEmpty(ownerLand.Value<string>("owner"));
                            plotMatched.owner_matic = ownerLand.Value<string>("owner");
                            plotMatched.building_id = ownerLand.Value<int?>("building_id") ?? 0;
                            plotMatched.building_level = ownerLand.Value<int?>("building_level") ?? 0;
                            plotMatched.building_type_id = ownerLand.Value<int?>("building_type_id") ?? 0;
                            plotMatched.token_id = ownerLand.Value<int?>("token_id") ?? 0;
                            plotMatched.on_sale = ownerLand.Value<bool?>("on_sale") ?? false;
                            plotMatched.current_price = plotMatched.on_sale ? building.GetSalePrice(ownerLand.Value<JToken>("sale_data"), worldType) : 0;
                            plotMatched.rented = ownerLand.Value<string>("renter") != null;
                            plotMatched.for_rent = plotMatched.rented == false ? building.GetRentPrice(ownerLand.Value<JToken>("rent_info"), worldType) : 0;
                            //plotMatched.abundance = ownerLand.Value<int?>("abundance") ?? 0;          // dont update abundance for related building plots - each plot can have own abundance
                            plotMatched.condition = ownerLand.Value<int?>("condition") ?? 0;

                            plotMatched.influence = ownerLand.Value<int?>("influence") ?? 0;
                            plotMatched.low_stamina_alert = citizen.CheckCitizenStamina(ownerLand.Value<JArray>("citizens"), plotMatched.building_type_id);
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
                logException(ex, String.Concat("PlotDB:UpdatePlotPartial() : Error Adding/update Plot token_id: ", tokenId));
            }

            return true;
        }

        private int GetApplicationBonus(int appNumber, JArray extraAppliances, int pos_x, int pos_y)
        {
            int appBonus = 0, appType=0;
            try
            {
                if (extraAppliances != null)
                {
                   
                    for (int count = 0; count < extraAppliances.Count; count++)
                    {
                        if (appNumber==4 && count == 0) {
                            appType = extraAppliances[count][1].Value<int?>() ?? 0;
                            break;
                        }
                        if (appNumber == 5 && count == 1)
                        {
                            appType = extraAppliances[count][1].Value<int?>() ?? 0;
                            break;
                        }                       
                    }

                    if (appType > 0) {
                        appBonus = appType switch
                        {
                            7 => 10,    // Red Sat (7) = 10% bonus
                            5 => 8,     // White Sat
                            8 => 6,     // Green Air con
                            1 => 5,     // White Air con
                            2 => 3,     // CCV White                            
                            4 => 2,     // Router Black 
                            3 => 1,     // Red Fire Alarm
                            _ => 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB.GetApplicationBonus() : Error calculating influance info for plot X:", pos_x, " Y:", pos_y));
            }

            return appBonus;
        }

        private int GetApplication123Bonus(JToken Appliance123, int pos_x, int pos_y)
        {
            int appBonus = 0, appType = 0;
            try
            {
                if (Appliance123 != null && Appliance123.Any())
                {
                    List<int?> values = Appliance123.Children().Values<int?>().ToList();

                    for (int appCount = 0; appCount < values.Count; appCount++)
                    {
                        appType = values[appCount] ?? 0;

                        if (appType > 0)
                        {
                            appBonus += appType switch
                            {
                                (int)APPLICATION.RED_SAT => 10,    // Red Sat (7) = 10% bonus
                                (int)APPLICATION.WHITE_SAT => 8,     // White Sat
                                (int)APPLICATION.GREEN_AIR_CON => 6,     // Green Air con
                                (int)APPLICATION.WHITE_AIR_CON => 5,     // White Air con
                                (int)APPLICATION.CCTV_RED => 4,     // CCTV RED 
                                (int)APPLICATION.CCTV_WHITE => 3,     // CCTV White                            
                                (int)APPLICATION.ROUTER_BLACK => 2,     // Router Black 
                                (int)APPLICATION.RED_FIRE_ALARM => 1,     // Red Fire Alarm
                                _ => 0
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB.GetApplication123Bonus() : Error calculating app bonus info for plot X:", pos_x, " Y:", pos_y));
            }

            return appBonus;
        }

        private int GetInfluenceInfoTotal(JToken influenceInfo, bool influencePOI, int pos_x, int pos_y, int buildType)
        {
            int influenceTotal = 0;
            int effectValue, effectBuildingType;
            try
            {
                if (influenceInfo != null)
                {
                    int baseInfluence = influenceInfo.Value<int?>("base") ?? 0;
                    influenceTotal += baseInfluence;

                    JArray effects = influenceInfo.Value<JArray>("effects");
                    if (effects != null)
                    {
                        for (int count =0; count < effects.Count; count++)
                        {
                            effectValue = effects[count].Value<int?>("value") ?? 0;
                            effectBuildingType = effects[count].Value<int?>("typeId") ?? 0;

                            // ignore any IP effect of matching buildingType if influencePOI flag is active
                            if (influencePOI == true && effectBuildingType == buildType && effectValue < 0)
                            {
                                continue;
                            }

                            influenceTotal += effectValue;                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB.GetInfluenceInfoTotal() : Error calculating influance info for plot X:", pos_x, " Y:", pos_y));
            }

            return influenceTotal;
        }

    }
}
