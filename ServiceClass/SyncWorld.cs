using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
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
            
            return string.Concat( "Sync Started at : ", DateTime.Now.ToShortTimeString() );

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
            int ownerCount =0;
            int resetCount = 0;
            string dbConnectionString = String.Empty;

            List<DistrictName> districtListMCPBasic = null;     // Init with null - to catch any service error and 3x retry pattern
            List<DistrictPerk> districtPerkListMCP = null;      // Init with null - to catch any service error and 3x retry pattern
            List<DistrictPerk> districtPerkList = new();

            int districtId = 0, saveCounter = 1, updateInstance = 0, retryCount =0;

            List<Plot> plotList, districtPlotList;
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
                while (districtListMCPBasic == null && retryCount < 3){

                    districtListMCPBasic = await districtManage.GetDistrictBasicFromMCP(true);
                    retryCount++;
                }
                if (retryCount > 1 && districtListMCPBasic !=null && districtListMCPBasic.Count > 0)
                {
                    _context.LogEvent( String.Concat("SyncWorld:SyncPlotData() : GetDistrictsFromMCP() retry successful - no ", retryCount));
                }
                districtListMCPBasic ??= new(); 


                // Store copy of current plots as archived plot state
                plotDB.ArchivePlots();
                _context.LogEvent(String.Concat("Nightly Sync: Plots Archived"));


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
                

                plotList = plotManage.CheckEmptyPlot(jobInterval, 0);                       // removes all empty plots from process list : if owner account has >2 empty plots - then get latest owner lands (1 SW) and remove all currently empty plots from process list
                plotList = plotManage.RemoveDistrictPlots(districtListMCPBasic, plotList);  // remove all unclaimed plots from districts that have not changed since last sync
                plotList = plotManage.RemoveMegaHugePlot(plotList);                         // remove all Mega & Huge plots that comprise a building, having only one plot representing the building.

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
                        plotManage.AddOrUpdatePlot(districtPlotList[plotIndex].plot_id, districtPlotList[plotIndex].pos_x, districtPlotList[plotIndex].pos_y,  false);
                        
                        await Task.Delay(jobInterval); 

                        saveCounter++;
                        
                        // Save every 40 plot update collection - improve performance on local db updates.
                        if (saveCounter >= 40 || saveDBOverride == true || plotIndex == plotList.Count -1)
                        {
                            _context.SaveWithRetry();
                            saveCounter = 0;
                        }                
                    }
                    _context.LogEvent(String.Concat(districtPlotList.Count, " plots for district ", districtId," processed"));


                    // All plots for district now updated, ready to sync district details with MCP, and generation a new set of owner summary records.
                    updateInstance = districtManage.UpdateDistrict( districtId ).Result;

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
                ownerManage = new(_context, worldType);
                ownerOfferDB = new(_context);
                citizenManage = new(_context, worldType);
                ownerCitizenDB = new(_context);
                petDB = new(_context);
                districtTaxChangeDB = new(_context);
                
                _context.LogEvent(String.Concat("Start Offer,Pet,Cit Sync"));

                // Add/deactive Owner Offers
                ownerManage.SyncOwner();        // Find New owners and owner names from nightly sync
                Dictionary<string, string> ownersList = ownerManage.GetOwners(true);  // Refresh list after nightly sync

                ownerOfferDB.SetOffersInactive();
                _context.LogEvent(String.Concat("All Owner Offers set to Inactive (recreate)"));

                foreach (string maticKey in ownersList.Keys)
                {

                    _context.SaveWithRetry();
                    _context.Dispose();
                    _context = new MetaverseMaxDbContext(worldType);
                    ownerManage = new(_context, worldType);
                    citizenManage = new(_context, worldType);

                    ownerManage.GetOwnerOffer(maticKey).Wait();
                    citizenManage.GetPetMCP(maticKey).Wait();
                    citizenManage.GetCitizenMCP(maticKey).Wait();

                    ownerCount++;                    

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
       
    }
}
