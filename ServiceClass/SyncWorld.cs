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
            PlotDB plotDB;
            DistrictDB districtDB;
            DistrictFundManage districtFundManage;
            DistrictWebMap districtWebMap;
            DistrictPerkManage districtPerkManage;
            DistrictPerkDB districtPerkDB;

            List<District> districtList;
            List<DistrictPerk> districtPerkList;

            int x = 0, y = 0, districtId = 0, saveCounter = 1, updateInstance = 0;
            List<Plot> plotList;

            try
            {
                ArrayList threadParameters = (ArrayList)parameters;                
                string dbConnectionString = (string)threadParameters[0];
                int worldType = (int)threadParameters[1];
                int jobInterval = (int)threadParameters[2];

                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();                
                _context = new MetaverseMaxDbContext(options.UseSqlServer(dbConnectionString).Options);

                districtPerkManage = new(_context);
                districtFundManage = new(_context);
                districtPerkDB = new(_context);
                districtWebMap = new(_context);
                districtDB = new(_context);
                plotDB = new(_context);

                districtList = districtDB.DistrictGetAll_Latest().ToList();

                // Store copy of current plots as archived plot state
                plotDB.ArchivePlots();

                // Get district perks
                districtPerkList = districtPerkManage.GetPerks().ToList();

                // Iterate each district, update all buildable plots within the district then sync district owners, and funds.
                for (int index = 0; index < districtList.Count; index++)
                {
                    districtId = districtList[index].district_id;
                    plotList = _context.plot.Where(r => r.district_id == districtId && r.land_type == 1).ToList();

                    for (int plotIndex = 0; plotIndex < plotList.Count; plotIndex++)
                    {

                        plotDB.AddOrUpdatePlot(plotList[plotIndex].pos_x, plotList[plotIndex].pos_y, _context, plotList[plotIndex].plot_id, false);

                        await Task.Delay(jobInterval);      // Typically minimum interval using this Delay thread method is about 1.5 seconds = 1500

                        saveCounter++;

                        // Save every 10 plot update collection- improve performance on local db updates.
                        if (saveCounter >= 10)
                        {
                            _context.SaveChanges();
                            saveCounter = 0;
                        }
                    }

                    // Save any pending plots before district sproc calls.
                    _context.SaveChanges();                    

                    // All plots for district now updated, ready to sync district details with MCP, and generation a new set of owner summary records.
                    updateInstance = districtWebMap.UpdateDistrict( districtId );

                    // Sync Funding for current district
                    districtFundManage.UpdateFundPriorDay(districtId);

                    // Assign Distrist update instance to related district perks (if any)
                    for (int perkIndex = 0; perkIndex < districtPerkList.Count; perkIndex++) 
                    {
                        if (districtPerkList[perkIndex].district_id == districtId)
                        {
                            districtPerkList[perkIndex].update_instance = updateInstance;
                        }
                    }

                }

                // Save Perk list after all districts updated.
                districtPerkDB.Save( districtPerkList );
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("SyncPlotData() : Error Adding Plot X:", x, " Y:", y));
                    _context.LogEvent(log);
                }
            }

            return;
        }
    }
}
