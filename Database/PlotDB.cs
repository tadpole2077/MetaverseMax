using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlTypes;
using MetaverseMax.BaseClass;
using MetaverseMax.ServiceClass;
using Microsoft.IdentityModel.Tokens;
using System;


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

        public List<BuildingToken> GetBuildingTokenIDList_ByOwnerMatic(string maticKey)
        {
            List<BuildingToken> buildingTokenIDlist = new();
            try
            {
                buildingTokenIDlist = _context.plot.Where(x => x.owner_matic == maticKey && 
                        x.building_type_id != (int)BUILDING_TYPE.EMPTY_LAND &&
                        x.building_type_id != (int)BUILDING_TYPE.POI &&
                        x.building_type_id != (int)BUILDING_TYPE.PARCEL
                    )
                    .Select(x => new BuildingToken { token_id = x.token_id, building_level = x.building_level, building_type_id = x.building_type_id})
                        .Distinct().ToList();
                    //.Select(r => r.token_id ).DistinctBy(r => ((uint)r)).ToList();
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PlotDB:getBuildingTokenIDList_ByOwnerMatic() : Error Getting plot token id list by ownerMaticKey", maticKey));
                _context.LogEvent(log);
            }

            return buildingTokenIDlist;
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
                _context.LogEvent(String.Concat("PlotDB:GetLastPlotUpdated() : Error Getting last updated Plot"));
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

                for (int index = 0; index < plotIPList.Count; index++)
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

        public long GetGlobalOfficeIP()
        {
            long globalIp = 0;
            int result;
            SqlInt64 ipValueholder;

            try
            {
                SqlParameter sqlGlobalIp = new SqlParameter
                {
                    ParameterName = "@office_global_ip",
                    SqlDbType = System.Data.SqlDbType.BigInt,
                    Direction = System.Data.ParameterDirection.Output,
                };

                //exec sproc to add set of owner summary rows matching instanct of district.
                result = _context.Database.ExecuteSqlRaw("EXEC @office_global_ip = dbo.sp_building_office_ip_get", new[] { sqlGlobalIp });

                ipValueholder = (SqlInt64)sqlGlobalIp.SqlValue;                 // SqlInt64 is not IConvertable interface so cant use Convert.ToInt64()
                globalIp = ipValueholder.IsNull ? 0 : (long)ipValueholder;      

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                _context.LogEvent(String.Concat("PlotDB::GetGlobalOfficeIP() : Error total IP for all (Global) Office block buildings"));
                _context.LogEvent(log);
            }

            return globalIp;
        }

        public RETURN_CODE UpdateActive(int tokenId, bool active, bool saveEvent)
        {
            List<Plot> plotList;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            try
            {
                plotList = GetPlotbyToken(tokenId);

                for (int index = 0; index < plotList.Count; index++)
                {
                    plotList[index].active = active;
                    plotList[index].last_updated = DateTime.UtcNow;
                }
                returnCode = RETURN_CODE.SUCCESS;

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::UpdateActive() : Error updating Plot using token_id : ", tokenId));
            }

            return returnCode;
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

        public RETURN_CODE UpdatePlotRank(int tokenId, decimal influenceEfficiency, bool usePrediction, bool saveEvent)
        {
            List<Plot> plotList;
            RETURN_CODE returnCode = RETURN_CODE.ERROR;

            try
            {
                plotList = GetPlotbyToken(tokenId);

                for (int index = 0; index < plotList.Count; index++)
                {
                    plotList[index].current_influence_rank = influenceEfficiency;
                    plotList[index].last_run_produce_predict = usePrediction;
                }
                returnCode = RETURN_CODE.SUCCESS;

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::UpdatePlotRank() : Error updating Plot using token_id : ", tokenId));
            }

            return returnCode;
        }

        public RETURN_CODE UpdateRelatedBuildingPlotSproc(int plotId)
        {
            RETURN_CODE returnCode = RETURN_CODE.ERROR;
            int result;

            try
            {
                result = _context.Database.ExecuteSqlInterpolated($"EXEC dbo.sp_plot_update_building { plotId }");

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::UpdateRelatedBuildingPlot() : Error updating related building Plots using target plot_id : ", plotId));
            }

            return returnCode;
        }

        public Plot AddPlot(Plot newPlot)
        {
            Plot newPlotEnity = null;

            try
            {
                newPlotEnity = _context.plot.Add(newPlot).Entity;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::AddPlot() : Error adding new Plots using token_id : ", newPlot.token_id));
            }

            return newPlotEnity;
        }

        public int GetCustomCountByDistrict(int districtId)
        {
            int count = 0;

            try
            {
                List<Plot> customPlot = _context.plot.Where(x => x.district_id == districtId && x.parcel_info_id > 0).ToList();
                count = customPlot.DistinctBy(x => x.parcel_info_id).Count();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::GetCustomCountByDistrict() : Error getting count for district : ", districtId.ToString()));
            }

            return count;
        }

        public int GetParcelCountByDistrict(int districtId)
        {
            int count = 0;

            try
            {
                List<Plot> parcelPlot = _context.plot.Where(x => x.district_id == districtId && x.parcel_id > 0).ToList();
                count = parcelPlot.DistinctBy(x => x.parcel_id).Count();

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB::GetParcelCountByDistrict() : Error getting count for district : ", districtId.ToString()));
            }

            return count;
        }       

        // SPECIAL CASE : Energy/Water Buildings - Huge and Mega,  may have different abundance levels on each plot.  Dont overwrite abundance.
        public int UpdateRelatedBuildingPlotLocal(Plot plotMaster)
        {
            List<Plot> buildingPlotList = null;

            try {
                // Find all related plots in this building - excluding master plot (copy the data from master)
                buildingPlotList = _context.plot.Where(x => x.token_id == plotMaster.token_id && x.plot_id != plotMaster.plot_id).ToList();

                for (int i = 0; i < buildingPlotList.Count; i++)
                {
                    buildingPlotList[i].update_type = (int)UPDATE_TYPE.COPY_MASTER;
                    buildingPlotList[i].last_updated = plotMaster.last_updated;

                    buildingPlotList[i].unclaimed_plot = plotMaster.unclaimed_plot;
#nullable enable
                    buildingPlotList[i].owner_nickname = plotMaster.owner_nickname;
#nullable disable
                    buildingPlotList[i].owner_matic = plotMaster.owner_matic;
                    buildingPlotList[i].owner_avatar_id = plotMaster.owner_avatar_id;
                    buildingPlotList[i].resources = plotMaster.resources;
                    buildingPlotList[i].building_id = plotMaster.building_id;
                    buildingPlotList[i].building_level = plotMaster.building_level;
                    buildingPlotList[i].building_type_id = plotMaster.building_type_id;
                    buildingPlotList[i].token_id = plotMaster.token_id;

                    buildingPlotList[i].on_sale = plotMaster.on_sale;
                    buildingPlotList[i].current_price = plotMaster.current_price;

                    buildingPlotList[i].for_rent = plotMaster.for_rent;
                    buildingPlotList[i].rented = plotMaster.rented;
                    //buildingPlotList[i].abundance = plotMaster.abundance;                     // dont update as each plot has unique abundance
                    buildingPlotList[i].building_abundance = plotMaster.building_abundance;
                    buildingPlotList[i].condition = plotMaster.condition;
                    buildingPlotList[i].influence_info = plotMaster.influence_info;
                    buildingPlotList[i].influence_bonus = plotMaster.influence_bonus;
                    buildingPlotList[i].current_influence_rank = plotMaster.current_influence_rank;
                    buildingPlotList[i].influence = plotMaster.influence;

                    buildingPlotList[i].influence_poi_bonus = plotMaster.influence_poi_bonus;
                    buildingPlotList[i].production_poi_bonus = plotMaster.production_poi_bonus;
                    buildingPlotList[i].is_perk_activated = plotMaster.is_perk_activated;
                    buildingPlotList[i].app_4_bonus = plotMaster.app_4_bonus;
                    buildingPlotList[i].app_5_bonus = plotMaster.app_5_bonus;
                    buildingPlotList[i].app_123_bonus = plotMaster.app_123_bonus;

                    buildingPlotList[i].citizen_count = plotMaster.citizen_count;
                    buildingPlotList[i].low_stamina_alert = plotMaster.low_stamina_alert;
                    buildingPlotList[i].action_id = plotMaster.action_id;

                    buildingPlotList[i].predict_produce = plotMaster.predict_produce;
                    buildingPlotList[i].last_run_produce = plotMaster.last_run_produce;
                    buildingPlotList[i].last_run_produce_date = plotMaster.last_run_produce_date;
                    buildingPlotList[i].last_run_produce_predict = plotMaster.last_run_produce_predict;


                    buildingPlotList[i].parcel_id = plotMaster.parcel_id;
                    buildingPlotList[i].parcel_info_id = plotMaster.parcel_info_id;

                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB:UpdateRelatedBuildingPlotLocal() : Error duplicating update for master building plot X:", plotMaster.pos_x, " Y:", plotMaster.pos_y));
            }

            return buildingPlotList == null ? 0 : buildingPlotList.Count;
        }

        public int GetApplicationBonus(int appNumber, JArray extraAppliances, int pos_x, int pos_y)
        {
            int appBonus = 0, appType = 0;
            try
            {
                if (extraAppliances != null)
                {

                    for (int count = 0; count < extraAppliances.Count; count++)
                    {
                        if (appNumber == 4 && count == 0)
                        {
                            appType = extraAppliances[count][1].Value<int?>() ?? 0;
                            break;
                        }
                        if (appNumber == 5 && count == 1)
                        {
                            appType = extraAppliances[count][1].Value<int?>() ?? 0;
                            break;
                        }
                    }

                    if (appType > 0)
                    {
                        appBonus = (int)FindApplicationBonusValue((APPLICATION_ID)appType);
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB.GetApplicationBonus() : Error calculating influance info for plot X:", pos_x, " Y:", pos_y));
            }

            return appBonus;
        }

        public int GetApplication123Bonus(JToken Appliance123, int pos_x, int pos_y)
        {
            int appBonusTotal = 0;

            try
            {
                if (Appliance123 != null && Appliance123.Any())
                {
                    List<int?> values = Appliance123.Children().Values<int?>().ToList();

                    for (int appCount = 0; appCount < values.Count; appCount++)
                    {
                        int appType = values[appCount] ?? 0;

                        if (appType > 0)
                        {
                            appBonusTotal += (int)FindApplicationBonusValue((APPLICATION_ID)appType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("PlotDB.GetApplication123Bonus() : Error calculating app bonus info for plot X:", pos_x, " Y:", pos_y));
            }

            return appBonusTotal;
        }

        APPLICATION_BONUS FindApplicationBonusValue(APPLICATION_ID appType)
        {
            APPLICATION_BONUS appBonus;
            appBonus = appType switch
            {
                APPLICATION_ID.RED_SAT => APPLICATION_BONUS.RED_SAT,
                APPLICATION_ID.GREEN_SAT => APPLICATION_BONUS.GREEN_SAT,
                APPLICATION_ID.WHITE_SAT => APPLICATION_BONUS.WHITE_SAT,
                APPLICATION_ID.GREEN_AIR_CON => APPLICATION_BONUS.GREEN_AIR_CON,
                APPLICATION_ID.WHITE_AIR_CON => APPLICATION_BONUS.WHITE_AIR_CON,
                APPLICATION_ID.RED_AIR_CON => APPLICATION_BONUS.RED_AIR_CON,
                APPLICATION_ID.CCTV_RED => APPLICATION_BONUS.CCTV_RED,
                APPLICATION_ID.CCTV_WHITE => APPLICATION_BONUS.CCTV_WHITE,
                APPLICATION_ID.ROUTER_BLACK => APPLICATION_BONUS.ROUTER_BLACK,
                APPLICATION_ID.RED_FIRE_ALARM => APPLICATION_BONUS.RED_FIRE_ALARM,
                _ => 0
            };

            return appBonus;
        }

        public int GetInfluenceInfoTotal(JToken influenceInfo, bool influencePOI, int pos_x, int pos_y, int buildType)
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
                        for (int count = 0; count < effects.Count; count++)
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
