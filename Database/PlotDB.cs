using MetaverseMax.ServiceClass;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.Database
{
    public partial class PlotDB : DbContext
    {
        private readonly MetaverseMaxDbContext _context;

        public PlotDB(DbContextOptions<PlotDB> options) : base(options)
        {
            //_context = ctx;
        }

        public PlotDB(MetaverseMaxDbContext _parentContext) 
        {
            _context = _parentContext;
        }

        public DbSet<Plot> Plots { get; set; }   // Links to a specific table in DB

        public Plot GetPlot(int plot_id)
        {
            ///var plots = from u in _context.plot where u.plot_id == plot_id select u;
            //if (plots.Count() == 1)
            //{
            //    return plots.First();
            //}
            return null;
        }

        public IEnumerable<Plot> PlotsGet()
        {
            List<Plot> plotList;

            plotList = this.Plots.OrderBy(x => x.plot_id ).ToList();

            /*
            using (var ctx = new SchoolDBEntities())
            {
                plotList = ctx.Plots
                                    .SqlQuery("Select * from Plot")
                                    .ToList<Plot>();
            }*/

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
                _context.LogEvent(String.Concat("GetLastPlotUpdated() : Error Getting last updated Plat"));
                _context.LogEvent(log);
            }
            
            return foundPlot == null ? new Plot() : foundPlot;
        }


        public static async void PollWorldPlots(object parameters)
        {
            MetaverseMaxDbContext _context = null;
            PlotDB plotDB;
            int x = 0, y =0;

            try
            {
                ArrayList threadParameters = (ArrayList)parameters;
                int startPosX = (int)threadParameters[0];
                int startPosY = (int)threadParameters[1];
                int endPosX = (int)threadParameters[2];
                int endPosY = (int)threadParameters[3];
                string dbConnectionString = (string)threadParameters[4];
                int jobInterval = (int)threadParameters[5];

                DbContextOptionsBuilder<MetaverseMaxDbContext> options = new();
                _context = new MetaverseMaxDbContext(options.UseSqlServer(dbConnectionString).Options);
                plotDB = new PlotDB(_context);

                // 250,000 plot locations - 1 second per plot - 69 hours. 100ms wait per plot = 7hrs.
                // Iterate though each of the plots in the target zone, add or update db row to match
                for ( x = Convert.ToInt32(startPosX); x <= Convert.ToInt32(endPosX); x++)
                {
                    for ( y = Convert.ToInt32(startPosY); y <= Convert.ToInt32(endPosY); y++)
                    {                        
                        await Task.Delay(jobInterval);      // Typically minimum interval using this Delay thread method is about 1.5 seconds

                        plotDB.AddOrUpdatePlot(x, y, _context, 0, true);

                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("PollWorldPlots() : Error Adding Plot X:", x, " Y:", y));
                    _context.LogEvent(log);
                }
            }

            return;
        }

        public int AddOrUpdatePlot(int pos_x, int pos_y, MetaverseMaxDbContext _context, int plotId, bool saveEvent)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes("{\"x\": \"" + pos_x.ToString() + "\",\"y\": \"" + pos_y.ToString() + "\"}");
            string content = string.Empty;
            Plot plotMatched;

            try
            {

                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/land/get");
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Ensure correct dispose of WebRespose IDisposable class even if exception
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader reader = new(response.GetResponseStream());
                    content = reader.ReadToEnd();
                }
                if (content.Length == 0)
                {
                    _context.plot.Add(new Plot()
                    {
                        pos_x = pos_x,
                        pos_y = pos_y,
                        notes = "No plot data returned from MCP",
                        last_updated = DateTime.Now
                    });
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);

                    // Based on the calleers passed plotId, either add a new plot or update an existing plot record.
                    if (plotId == 1) {

                        _context.plot.Add(new Plot()
                        {
                            cell_id = jsonContent.Value<int?>("cell_id") ?? 0,
                            district_id = jsonContent.Value<int?>("region_id") ?? 0,
                            land_type = jsonContent.Value<int?>("land_type") ?? 0,
                            pos_x = pos_x,
                            pos_y = pos_y,
                            last_updated = DateTime.Now,
                            current_price = 0,
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
                            abundance = jsonContent.Value<int?>("abundance") ?? 0
                        });
                    }
                    else
                    {
                        plotMatched = _context.plot.Find(plotId);

                        plotMatched.last_updated = DateTime.Now;
                        plotMatched.current_price = 0;
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
                        plotMatched.abundance = jsonContent.Value<int?>("abundance") ?? 0;
                    }
                }

                if (saveEvent)
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("AddOrUpdatePlot() : Error Adding/update Plot X:", pos_x, " Y:", pos_y));
                    _context.LogEvent(log);
                }
            }

            return 0;
        }
    }
}
