using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlotController : ControllerBase
    {
        private readonly ILogger<PlotController> _logger;
        private readonly MetaverseMaxDbContext _context;

        public PlotController(MetaverseMaxDbContext context, ILogger<PlotController> logger)        
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public PollingPlot Get([FromQuery] string start_pos_x, [FromQuery] string start_pos_y, [FromQuery] string end_pos_x, [FromQuery] string end_pos_y, [FromQuery] string secure_token, [FromQuery] string interval)
        {
            PollingPlot pollingPlot = new();
            PlotDB plotDB = new(_context);

            try
            {
                // As this service could be abused as a DDOS a security token is needed.
                if (secure_token == null || !secure_token.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return pollingPlot;
                }
                int jobInterval = Convert.ToInt32(string.IsNullOrEmpty(interval) ? 150 : interval);


                ArrayList threadParameters = new();
                threadParameters.Add(Convert.ToInt32(start_pos_x));
                threadParameters.Add(Convert.ToInt32(start_pos_y));
                threadParameters.Add(Convert.ToInt32(end_pos_x));
                threadParameters.Add(Convert.ToInt32(end_pos_y));
                threadParameters.Add(_context.Database.GetConnectionString());
                threadParameters.Add(jobInterval);


                // Assign delegate function type to parameter thread object           
                ParameterizedThreadStart pollWorldPlots = new(PlotDB.PollWorldPlots);

                // Create a new thread utilizing the parameter thread object
                Thread tPollWorldPlots = new(pollWorldPlots)
                {
                    //*** next we set the priority of our thread to normal default setting
                    Priority = ThreadPriority.Normal
                };

                //*** start execution of thread
                tPollWorldPlots.Start(threadParameters);

                // Select type query using LINQ returning a collection of row matching condition - selecting first row.               
                pollingPlot.last_plot_updated = plotDB.GetLastPlotUpdated();
                pollingPlot.status = "Polling World Started";
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }
            // The property 'Plot.last_updated' could not be mapped because it is of type 'Nullable<SqlString>
            return pollingPlot;
        }   

    }
}
