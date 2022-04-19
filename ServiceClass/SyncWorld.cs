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
    public class SyncWorld
    {
        private readonly MetaverseMaxDbContext _context;
        private Common common = new();

        public static int jobInterval = 1;
        public static int jobIntervalRequested = 1;
        public static bool saveDBOverride = false; 

        public SyncWorld(MetaverseMaxDbContext _contextService)
        {
            _context = _contextService;
        }

        // Nightly Data Sync of all Open District plots that are buildable. Iterates through all districts, and 2nd tier of all plots within district
        public string SyncActiveDistrictPlot(WORLD_TYPE worldType, string secureToken, int interval)
        {
            // As this service could be abused as a DDOS a security token is needed.
            if (!secureToken.Equals("JUST_SIMPLE_CHECK123"))
            {
                return "";
            }

            if (worldType == WORLD_TYPE.TRON)
            {
                ArrayList threadParameters = new();
                threadParameters.Add(_context.Database.GetConnectionString());
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

            }

            return string.Concat( "Sync Started at : ", DateTime.Now.ToShortTimeString() );
        }

        public static async void SyncPlotData(object parameters)
        {
            MetaverseMaxDbContext _context = null;
            DBLogger dbLogger = new(null);
            PlotDB plotDB;
            DistrictDB districtDB;
            DistrictTaxChangeDB districtTaxChangeDB;
            DistrictFundManage districtFundManage;
            DistrictWebMap districtWebMap;
            DistrictPerkManage districtPerkManage;
            DistrictPerkDB districtPerkDB;
            CitizenManage citizenManage;
            OwnerManage ownerManage;
            OwnerOfferDB ownerOfferDB;
            PetDB petDB;
            OwnerCitizenDB ownerCitizenDB;

            List<DistrictName> districtList = new();
            List<DistrictPerk> districtPerkListMCP = new();
            List<DistrictPerk> districtPerkList = new();

            int districtId = 0, saveCounter = 1, updateInstance = 0, retryCount =0;
            List<Plot> plotList;

            try
            {                
                ArrayList threadParameters = (ArrayList)parameters;                
                string dbConnectionString = (string)threadParameters[0];
                int worldType = (int)threadParameters[1];
                jobIntervalRequested = jobInterval = (int)threadParameters[2];
                
                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();                
                _context = new MetaverseMaxDbContext(options.UseSqlServer(dbConnectionString).Options);

                BuildingManage buildingManage = new(_context);
                dbLogger = new(_context);
                districtPerkManage = new(_context);
                districtFundManage = new(_context);
                districtPerkDB = new(_context);
                districtWebMap = new(_context);
                districtDB = new(_context);
                districtTaxChangeDB = new(_context);
                plotDB = new(_context);
                ownerManage = new(_context);
                ownerOfferDB = new(_context);
                petDB = new(_context);
                ownerCitizenDB = new(_context);
                citizenManage = new(_context);

                dbLogger.logInfo(String.Concat("Start Nightly Sync"));

                // Update All Districts from MCP, as a new district may have opened.  Attempt 3 times, before failing - as no districts mean no plot updates
                retryCount = 0;
                while (districtList.Count == 0 && retryCount < 3){

                    districtList = await districtWebMap.GetDistrictsFromMCP(true);
                    retryCount++;
                }
                if (retryCount > 1 && districtList.Count > 0)
                {
                    dbLogger.logException(new Exception(), String.Concat("SyncWorld:SyncPlotData() : GetDistrictsFromMCP() retry successful - no ", retryCount));
                }

                // Store copy of current plots as archived plot state
                plotDB.ArchivePlots();
                dbLogger.logInfo(String.Concat("Nightly Sync: Plots Archived"));

                // Get district perks, attempt 3 times.
                retryCount = 0;
                while (districtPerkListMCP.Count == 0 && retryCount < 3)
                {
                    districtPerkListMCP = districtPerkManage.GetPerks().Result.ToList();
                    retryCount++;
                }
                if (retryCount > 1 && districtPerkListMCP.Count > 0)
                {
                    dbLogger.logException(new Exception(), String.Concat("SyncWorld:SyncPlotData() : GetPerks() retry successful - no ", retryCount));
                }

                // Iterate each district, update all buildable plots within the district then sync district owners, and funds.
                for (int index = 0; index < districtList.Count; index++)
                {
                    districtId = districtList[index].district_id;
                    plotList = _context.plot.Where(r => r.district_id == districtId && r.land_type == 1).ToList();

                    for (int plotIndex = 0; plotIndex < plotList.Count; plotIndex++)
                    {

                        plotDB.AddOrUpdatePlot(plotList[plotIndex].pos_x, plotList[plotIndex].pos_y, plotList[plotIndex].plot_id, false);

                        await Task.Delay(jobInterval); 

                        saveCounter++;

                        // Save every 10 plot update collection- improve performance on local db updates.
                        if (saveCounter >= 10 || saveDBOverride == true)
                        {
                            _context.SaveChanges();
                            saveCounter = 0;
                        }
                    }

                    // Save any pending plots before district sproc calls.
                    _context.SaveChanges();                    

                    // All plots for district now updated, ready to sync district details with MCP, and generation a new set of owner summary records.
                    updateInstance = districtWebMap.UpdateDistrict( districtId ).Result;

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

                if (districtList.Count > 0)
                {
                    _context.ActionUpdate(ACTION_TYPE.PLOT);
                }

                ownerManage.SyncOwner();        // New owners found from nightly sync

                dbLogger.logInfo(String.Concat("Start Offer,Pet,Cit Sync"));
                
                // Add/deactive Owner Offers                             
                Dictionary<string, string> ownersList = ownerManage.GetOwners(true);  // Refresh list after nightly sync
                ownerOfferDB.SetOffersInactive();

                dbLogger.logInfo(String.Concat("All Owner Offers set to Inactive (recreate)"));

                foreach (string maticKey in ownersList.Keys)
                {
                    await ownerManage.GetOwnerOffer(true, maticKey);

                    await citizenManage.GetPetMCP(maticKey);

                    await citizenManage.GetCitizenMCP(maticKey);

                    dbLogger.logInfo(String.Concat("Owner Offer, Pet, Citizen Updated for : ", maticKey));

                    // Add a delay of 2 seconds if active user.
                    if (saveDBOverride == true)
                    {
                        await Task.Delay(2000);      // 100ms delay to help prevent server side kicks
                    }
                }
                _context.ActionUpdate(ACTION_TYPE.CITIZEN);
                _context.ActionUpdate(ACTION_TYPE.OFFER);
                _context.ActionUpdate(ACTION_TYPE.PET);
                dbLogger.logInfo(String.Concat("End Offer,Pet,Cit Sync"));

                petDB.UpdatePetCount();
                ownerCitizenDB.UpdateCitizenCount();
                districtTaxChangeDB.UpdateTaxChanges();

                dbLogger.logInfo(String.Concat("End Tax Change Sync"));

                dbLogger.logInfo(String.Concat("Start IP Ranking Sync"));

                await buildingManage.UpdateIPRanking(jobInterval);

                dbLogger.logInfo(String.Concat("End Nightly Sync"));

            }
            catch (Exception ex)
            {
                dbLogger.logException(ex, String.Concat("SyncPlotData() : Error Processing Sync"));                
            }
            
            return;
        }

        public static void SyncPlotData_Reset()
        {
            jobInterval = jobIntervalRequested;
            saveDBOverride = false;
        }
    }
}
