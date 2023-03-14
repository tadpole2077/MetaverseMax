using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    public class SyncWorld : ServiceBase
    {
        private Common common = new();

        public static int jobInterval = 1;
        public static bool saveDBOverride = false;
        public static bool syncInProgress = false;

        public SyncWorld(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

        // Nightly Data Sync of all Open District plots that are buildable. Iterates through all districts, and 2nd tier of all plots within district
        public string SyncActiveDistrictPlot(string secureToken, int interval)
        {
            // As this service could be abused as a DDOS a security token is needed.
            if (!secureToken.Equals("JUST_SIMPLE_CHECK123"))
            {
                return "";
            }

            ArrayList threadParameters = new();
            threadParameters.Add(worldType);
            threadParameters.Add(interval);

            // Assign delegate function type to parameter thread object           
            ParameterizedThreadStart pollWorldPlots = new(SyncPlotData);

            // Create a new thread utilizing the parameter thread object
            Thread tPollWorldPlots = new(pollWorldPlots)
            {
                //*** next we set the priority of our thread to normal default setting
                Priority = ThreadPriority.Normal
            };

            //*** start execution of thread
            tPollWorldPlots.Start(threadParameters);

            return string.Concat("Sync Started at : ", DateTime.Now.ToShortTimeString());

        }

        public static void SyncPlotData(object parameters)
        {
            ArrayList threadParameters = (ArrayList)parameters;
            WORLD_TYPE worldType = (WORLD_TYPE)threadParameters[0];
            DateTime startTime;

            try
            {
                RETURN_CODE response;
                int jobInterval = (int)threadParameters[1];

                MetaverseMaxDbContext _context = new MetaverseMaxDbContext(worldType);
                SyncWorld syncWorld = new(_context, worldType);
                SyncDB syncDB = new(_context);

                List<Sync> syncJobs = syncDB.Get(true);
                _context.Dispose();

                for (int counter = 0; counter < syncJobs.Count; counter++)
                {
                    startTime = DateTime.Now;
                    response = Task.Run(() => syncWorld.SyncRun(jobInterval, (WORLD_TYPE)syncJobs[counter].world)).Result;

                    using (_context = new MetaverseMaxDbContext(worldType))
                    {
                        SyncHistoryDB syncHistoryDB = new(_context);
                        syncHistoryDB.Add(syncJobs[counter].world, startTime, DateTime.Now);
                    }
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(worldType);
                dbLogger.logException(ex, String.Concat("SyncWorld::SyncPlotData() : Error Processing Sync"));
            }

            return;
        }

        // POI active until only updated by this call, and nightly data-sync should be the only caller. As POI and monument status updates impact many other plots IP and require deeper examination
        // Need to identify - if POI changed since last update and if that change impacts plots IP.
        //      a) (NO CHANGE) active_until was already a future date - just extended.   No change to plot IP's
        //      b) (DEACTIVATED) active_until has expired at the time of this call.   May have impact on plot IP's if previouisly including effect of POI/Monument
        //      c) (ACTIVATED) active_until was expired but recently extended and currently active(identified during nightly sync).  May have impact on plot IP's
        //
        // Key Logic: plot.last_updated<plot.poi_active_until then POI/Monument was active on last data sync instance
        // Condition: POI / Monument plot records only allow plot.last_updated changed by nightly sync.
        public int UpdatePoiBuildings(List<DistrictName> districtListMCPBasic, List<OwnerChange> ownerChange, MetaverseMaxDbContext customContext, WORLD_TYPE passedWorldType)
        {
            List<Plot> poiList, poiBuildingPlot;
            List<int> poiListToken;
            DistrictName targetDistrict;
            OwnerChange targetOwnerChange;
            LAND_TYPE landType = worldType switch { WORLD_TYPE.TRON => LAND_TYPE.TRON_BUILDABLE_LAND, WORLD_TYPE.BNB => LAND_TYPE.BNB_BUILDABLE_LAND, WORLD_TYPE.ETH => LAND_TYPE.TRON_BUILDABLE_LAND, _ => LAND_TYPE.TRON_BUILDABLE_LAND };
            JToken poiData;
            JArray poiArray;
            DateTime? currentActiveUntil, localStoredActiveUntil, lastUpdated;
            int districtChangeCount = 0;
            bool poiLastSync_WasActive = false;

            try
            {
                BuildingManage buildingManage = new(customContext, passedWorldType);
                poiList = customContext.plot.Where(r => r.land_type == (int)landType && r.building_type_id == (int)BUILDING_TYPE.POI).ToList();

                // Get token list of all poi buildings from poi plot list
                poiListToken = poiList.Select( r=>r.token_id ).DistinctBy( r=>((uint)r)).ToList();

                if (poiListToken.Count > 0)
                {
                    poiArray = Task.Run(() => buildingManage.GetPoiMCP(poiListToken)).Result;

                    // Iterate each poi building - Check if active_until date changed since last sync, update and tag affected district
                    for (int counter = 0; counter < poiArray.Count; counter++)
                    {
                        poiData = poiArray[counter];
                        currentActiveUntil = common.ConvertDateTimeUTC(poiData.Value<string>("active_until"));

                        poiBuildingPlot = poiList.Where(x => x.token_id == (poiData.Value<int?>("token_id") ?? 0)).ToList();

                        if (poiBuildingPlot != null)
                        {
                            localStoredActiveUntil = poiBuildingPlot[0].poi_active_until;
                            lastUpdated = poiBuildingPlot[0].last_updated;
                            poiLastSync_WasActive = poiBuildingPlot[0].poi_active_until == null ? false : poiBuildingPlot[0].poi_active_until > poiBuildingPlot[0].last_updated;


                            // Update each plot within the POI building
                            for (int plotIndex = 0; plotIndex < poiBuildingPlot.Count; plotIndex++)
                            {
                                // Update POI active_until if changed
                                if (currentActiveUntil > poiBuildingPlot[plotIndex].poi_active_until ||
                                    (poiBuildingPlot[plotIndex].poi_active_until == null && currentActiveUntil != null))
                                {
                                    poiBuildingPlot[plotIndex].last_updated = DateTime.UtcNow;
                                    poiBuildingPlot[plotIndex].poi_active_until = common.ConvertDateTimeUTC(poiData.Value<string>("active_until"));
                                }
                            }



                            // Tag district change
                            if (new[] { (int)BUILDING_SUBTYPE.ENERGY_LANDMARK,
                                (int)BUILDING_SUBTYPE.INDUSTRY_LANDMARK,
                                (int)BUILDING_SUBTYPE.PRODUCTION_LANDMARK,
                                (int)BUILDING_SUBTYPE.RESIDENTIAL_LANDMARK,
                                (int)BUILDING_SUBTYPE.OFFICE_LANDMARK}.Contains(poiBuildingPlot[0].building_id))
                            {
                                targetDistrict = districtListMCPBasic.Where(x => x.district_id == poiBuildingPlot[0].district_id).FirstOrDefault();

                                if (targetDistrict != null && targetDistrict.poi_activated == false)
                                {
                                    targetDistrict.poi_activated = (!poiLastSync_WasActive && currentActiveUntil > DateTime.UtcNow);
                                    targetDistrict.poi_deactivated = (poiLastSync_WasActive && currentActiveUntil <= DateTime.UtcNow);
                                    districtChangeCount++;
                                }
                            }
                            else
                            {

                                // Tag Account change - if any account owned monument state changed, then record it. TO_DO IMPROVE to per district.
                                targetOwnerChange = ownerChange.Where(x => x.owner_matic_key == poiBuildingPlot[0].owner_matic).FirstOrDefault();
                                if (targetOwnerChange == null)
                                {
                                    ownerChange.Add(new OwnerChange()
                                    {
                                        owner_matic_key = poiBuildingPlot[0].owner_matic,
                                        monument_activated = (!poiLastSync_WasActive && currentActiveUntil > DateTime.UtcNow),
                                        monument_deactivated = (poiLastSync_WasActive && currentActiveUntil <= DateTime.UtcNow)
                                    });
                                }
                                else
                                {
                                    targetOwnerChange.monument_activated = targetOwnerChange.monument_activated == false ?
                                        !poiLastSync_WasActive && currentActiveUntil > DateTime.UtcNow : true;

                                    targetOwnerChange.monument_deactivated = targetOwnerChange.monument_deactivated == false ?
                                        poiLastSync_WasActive && currentActiveUntil <= DateTime.UtcNow : true;
                                }
                            }
                        }
                    }

                    customContext.SaveWithRetry();
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(worldType);
                dbLogger.logException(ex, String.Concat("SyncWorld::UpdatePoiBuildings() : Error updating POI buildings"));
            }

            return districtChangeCount;
        }

        public async Task<RETURN_CODE> SyncRun(int jobInterval, WORLD_TYPE worldType)
        {
            MetaverseMaxDbContext _context = null;
            PlotDB plotDB;
            DistrictManage districtManage;
            DistrictTaxChangeDB districtTaxChangeDB;
            DistrictFundManage districtFundManage;
            DistrictPerkManage districtPerkManage;
            DistrictPerkDB districtPerkDB;
            CitizenManage citizenManage;
            OwnerManage ownerManage;
            OwnerOfferDB ownerOfferDB;
            PetDB petDB;
            OwnerCitizenDB ownerCitizenDB;
            PlotManage plotManage;
            Plot plotUpdated;
            int ownerCount = 0, districtChangeCount = 0, accountPlotRemovedCount=0, unclaimedPlotRemovedCount = 0, megaHugeplotsRemoved =0;
            string dbConnectionString = String.Empty;

            List<DistrictName> districtListMCPBasic = null;     // Init with null - to catch any service error and 3x retry pattern
            List<DistrictPerk> districtPerkListMCP = null;      // Init with null - to catch any service error and 3x retry pattern
            List<DistrictPerk> districtPerkList = new();
            List<OwnerChange> ownerChangeList = new();

            int districtId = 0, saveCounter = 1, updateInstance = 0, retryCount = 0;

            List<Plot> plotList, districtPlotList;
            LAND_TYPE landType = worldType switch { WORLD_TYPE.TRON => LAND_TYPE.TRON_BUILDABLE_LAND, WORLD_TYPE.BNB => LAND_TYPE.BNB_BUILDABLE_LAND, WORLD_TYPE.ETH => LAND_TYPE.TRON_BUILDABLE_LAND, _ => LAND_TYPE.TRON_BUILDABLE_LAND };

            syncInProgress = true;

            try
            {
                _context = new MetaverseMaxDbContext(worldType);

                BuildingManage buildingManage = new(_context, worldType);
                districtManage = new(_context, worldType);
                districtPerkManage = new(_context, worldType);
                plotDB = new(_context, worldType);
                plotManage = new(_context, worldType);

                _context.LogEvent(String.Concat("Start Nightly Sync"));
                saveDBOverride = false;

                // Update All Districts from MCP, as a new district may have opened.  Attempt 3 times, before failing - as no districts mean no plot updates
                retryCount = 0;
                while (districtListMCPBasic == null && retryCount < 3) {

                    districtListMCPBasic = await districtManage.GetDistrictBasicFromMCP(true);
                    retryCount++;
                }
                if (retryCount > 1 && districtListMCPBasic != null && districtListMCPBasic.Count > 0)
                {
                    _context.LogEvent(String.Concat("SyncWorld:SyncPlotData() : GetDistrictsFromMCP() retry successful - no ", retryCount));
                }
                districtListMCPBasic ??= new();


                // Store copy of current plots as archived plot state
                plotDB.ArchivePlots();
                districtManage.ArchiveOwnerSummaryDistrict();
                _context.LogEvent(String.Concat("Nightly Sync: Plots Archived, OwnerSummaryDistrict Archived"));


                // Get district perks, attempt 3 times.
                retryCount = 0;
                while (districtPerkListMCP == null && retryCount < 3)
                {
                    var holder = districtPerkManage.GetPerks().Result;
                    if (holder != null)
                    {
                        districtPerkListMCP = holder.ToList();
                    }
                    retryCount++;
                }
                if (retryCount > 1 && districtPerkListMCP != null && districtPerkListMCP.Count > 0)
                {
                    _context.LogEvent(String.Concat("SyncWorld:SyncPlotData() : GetPerks() retry successful - no ", retryCount));
                }
                districtPerkListMCP ??= new();  // CHECK if Error occured and logged - still no succuess after retry, then set List and continue with sync, wont be able to update plots, but other components can complete.


                districtChangeCount = UpdatePoiBuildings(districtListMCPBasic, ownerChangeList, _context, worldType);               // Find changes and Update all POI plots active_until date

                plotList = _context.plot.Where(r => r.land_type == (int)LAND_TYPE.BNB_BUILDABLE_LAND || r.land_type == (int)LAND_TYPE.TRON_BUILDABLE_LAND).ToList();
                unclaimedPlotRemovedCount = plotManage.RemoveDistrictPlots(districtListMCPBasic, plotList);     // remove all unclaimed plots from districts that have not changed since last sync
                accountPlotRemovedCount = plotManage.RemoveAccountPlot(jobInterval, ownerChangeList, districtListMCPBasic, plotList);             // removes all empty plots from process list : if owner account has >2 empty plots - then get latest owner lands (1 SW) and remove all currently empty plots from process list                
                megaHugeplotsRemoved = plotManage.RemoveMegaHugePlot(plotList);                                 // remove all Mega & Huge plots that comprise a building, having only one plot representing the building.

                _context.LogEvent(String.Concat("Plot filter:  a) unclaimed Plots: ", unclaimedPlotRemovedCount, " b) claimed Plots: ", accountPlotRemovedCount, "  c) huge+mega plots: ", megaHugeplotsRemoved, "  d) Plots to process: ", plotList.Count));

                // Iterate each district, update all "buildable plots" within the district then sync district owners, and funds.
                for (int index = 0; index < districtListMCPBasic.Count; index++)
                {
                    // Reset DB Context before each DISTRICT plot set sync (as db context may auto close/drop after a set period of time)
                    _context.SaveWithRetry();
                    saveCounter = 0;
                    _context.Dispose();
                    _context = new MetaverseMaxDbContext(worldType);
                    plotDB = new(_context, worldType);
                    plotManage = new(_context, worldType);
                    districtPerkDB = new(_context);
                    districtManage = new(_context, worldType);
                    districtFundManage = new(_context, worldType);                    

                    // Extract list of plots for current target district
                    districtId = districtListMCPBasic[index].district_id;
                    districtPlotList = plotList.Where(r => r.district_id == districtId).ToList();

                    for (int plotIndex = 0; plotIndex < districtPlotList.Count; plotIndex++)
                    {
                        // Update Plot + Store owner change details extracted from plot
                        plotUpdated = plotManage.AddOrUpdatePlot(districtPlotList[plotIndex].plot_id, districtPlotList[plotIndex].pos_x, districtPlotList[plotIndex].pos_y, false);

                        // Track current MCP owner avator and id (only need 1 update per account - MCP displays latet on each plot of owner account)
                        // Purpose to update all owner account to curret name and avator [after all plots processed]
                        if (ownerChangeList != null)
                        {
                            // Record Account avator/name change
                            OwnerChange targetOwnerChange = ownerChangeList.Where(x => x.owner_matic_key == plotUpdated.owner_matic).FirstOrDefault();
                            if (targetOwnerChange == null)
                            {
                                ownerChangeList.Add(new OwnerChange()
                                {
                                    owner_matic_key = plotUpdated.owner_matic,
                                    monument_activated = false,                 // if owner pre-existed in list then POI change = true.
                                    owner_name = plotUpdated.owner_nickname,
                                    owner_avatar_id = plotUpdated.owner_avatar_id
                                });
                            }
                            else if (targetOwnerChange.owner_avatar_id != plotUpdated.owner_avatar_id)
                            {
                                // Check if owner avatar changed, then save change to bulk update all owner plots later in process
                                targetOwnerChange.owner_avatar_id = plotUpdated.owner_avatar_id;
                                targetOwnerChange.owner_name = plotUpdated.owner_nickname;
                            }
                        }

                        await Task.Delay(jobInterval);
                        saveCounter++;

                        // Save every 40 plot update collection - improve performance on local db updates.
                        if (saveCounter >= 40 || saveDBOverride == true || plotIndex == plotList.Count - 1)
                        {
                            _context.SaveWithRetry();
                            saveCounter = 0;
                        }
                    }
                    _context.LogEvent(String.Concat(districtPlotList.Count, " plots for district ", districtId, " processed"));


                    // All plots for district now updated, ready to sync district details with MCP, and generation a new set of owner summary records.
                    updateInstance = districtManage.UpdateDistrict(districtId).Result;

                    // Sync Funding for current district
                    await districtFundManage.UpdateFundPriorDay(districtId);

                    // Assign Distrist update instance to related district perks (if any)
                    for (int perkIndex = 0; perkIndex < districtPerkListMCP.Count; perkIndex++)
                    {
                        if (districtPerkListMCP[perkIndex].district_id == districtId)
                        {
                            districtPerkList.Add(new DistrictPerk() {
                                district_id = districtId,
                                perk_id = districtPerkListMCP[perkIndex].perk_id,
                                perk_level = districtPerkListMCP[perkIndex].perk_level,
                                update_instance = updateInstance
                            });
                        }
                    }
                    // Save Perk list after each district update, to better support live use of db during sync.
                    districtPerkDB.Save(districtPerkList);
                    districtPerkList.Clear();

                }

                if (districtPerkListMCP.Count > 0)
                {
                    _context.ActionUpdate(ACTION_TYPE.PLOT);
                }


                // Refresh db context - supposed to be a short term use + when using async tasks seems to be dropped over time (not sure what triggers the drop - perhaps memory leaks)
                _context.SaveWithRetry();
                _context.Dispose();
                _context = new MetaverseMaxDbContext(worldType);
                saveCounter = 0;

                ownerManage = new(_context, worldType);
                ownerOfferDB = new(_context);
                citizenManage = new(_context, worldType);

                _context.LogEvent(String.Concat("Start Offer,Pet,Cit Sync"));
                _context.LogEvent(String.Concat("Owner change list count: ", ownerChangeList.Count));

                // Add/deactive Owner Offers
                ownerManage.SyncOwner(ownerChangeList);                                             // Find New owners and owner names from nightly sync                
                Dictionary<string, OwnerAccount> ownersList = ownerManage.GetOwners(true);          // Refresh list after nightly sync

                ownerOfferDB.SetOffersInactive();
                _context.LogEvent(String.Concat("All Owner Offers set to Inactive (recreate)"));

                foreach (string maticKey in ownersList.Keys)
                {
                    // Save every 40 owner account - improve performance on local db updates.
                    if (saveCounter >= 40 || saveDBOverride == true)
                    {
                        _context.SaveWithRetry();
                        saveCounter = 0;

                        _context.Dispose();
                        _context = new MetaverseMaxDbContext(worldType);
                        ownerManage = new(_context, worldType);
                        citizenManage = new(_context, worldType);
                    }

                    ownerManage.GetOwnerOfferMCP(maticKey).Wait();
                    citizenManage.GetPetMCP(maticKey).Wait();
                    citizenManage.GetOwnerCitizenCollectionMCP(maticKey).Wait();

                    ownerCount++;
                    saveCounter++;

                    // Add a stardard delay between service calls component, or 2 seconds per cycle if any active users identified.
                    await Task.Delay(saveDBOverride == true ? 2000 : jobInterval);
                }

                _context.LogEvent(String.Concat("Owner (Offer, Pet, Citizen) Accounts Updated count : ", ownerCount));
                _context.ActionUpdate(ACTION_TYPE.CITIZEN);
                _context.ActionUpdate(ACTION_TYPE.OFFER);
                _context.ActionUpdate(ACTION_TYPE.PET);
                _context.LogEvent(String.Concat("End Offer,Pet,Cit Sync"));
                _context.SaveWithRetry();

                // refresh db contexts - in case of long running tasks casusing auto expiry
                ownerCitizenDB = new(_context);
                petDB = new(_context);
                districtTaxChangeDB = new(_context);

                petDB.UpdatePetCount();
                ownerCitizenDB.UpdateCitizenCount();

                districtTaxChangeDB.UpdateTaxChanges();
                _context.LogEvent(String.Concat("End Tax Change Sync"));

                // Refresh db context - supposed to be a short term use + when using async tasks seems to be dropped over time (not sure what triggers the drop - perhaps memory leaks)
                _context.SaveWithRetry();
                _context.Dispose();
                _context = new MetaverseMaxDbContext(worldType);
                buildingManage = new(_context, worldType);

                _context.LogEvent(String.Concat("Start IP Ranking Sync"));
                buildingManage.UpdateIPRanking(jobInterval);
                _context.LogEvent(String.Concat("End IP Ranking Sync"));

                _context.LogEvent(String.Concat("End Nightly Sync"));

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("SyncWorld::SyncPlotData() : Error Processing Sync"));
            }

            syncInProgress = false;
            if (_context != null && _context.IsDisposed() == false)
            {
                _context.Dispose();
            }

            return RETURN_CODE.SUCCESS;
        }
   
        public string UnitTest_POIActive()
        {
            DistrictManage districtManage = new(_context, worldType);
            int districtChangeCount = 0;

            List<DistrictName> districtListMCPBasic = Task.Run(() => districtManage.GetDistrictBasicFromMCP(true)).Result;
            List<OwnerChange> ownerChangesList = new();

            districtChangeCount = UpdatePoiBuildings(districtListMCPBasic, ownerChangesList, _context, worldType);

            return string.Concat("District with POI changed count: ", districtChangeCount);
        }
    }
}
