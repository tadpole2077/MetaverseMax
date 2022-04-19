using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        public PollingPlot Get([FromQuery] QueryParametersGetPlotMatric parameters)
        {
            PollingPlot pollingPlot = new();
            PlotDB plotDB;
            SyncWorld syncWorld;

            try
            {
                plotDB = new(_context);
                syncWorld = new(_context);

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

        [HttpGet("UpdatePlotSync")]
        public IActionResult UpdatePlotSync([FromQuery] QueryParametersPlotSync parameters)
        {
            SyncWorld syncWorld = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(syncWorld.SyncActiveDistrictPlot( (WORLD_TYPE)parameters.world_type, parameters.secure_token, parameters.interval) );
            }

            return BadRequest("Sync Failed");       // 400 Error     
        }

        [HttpGet("UpdatePlotSingle")]
        public IActionResult UpdatePlotSingle([FromQuery] QueryParametersPlotSingle parameters)
        {
            PlotDB plotDB = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(plotDB.AddOrUpdatePlot(parameters.posX, parameters.posY, parameters.plot_id, true));
            }

            return BadRequest("Sync Failed");       // 400 Error     
        }

        [HttpGet("BuildingIPbyTypeGet")]
        public IActionResult BuildingIPbyTypeGet([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() =>  buildingManage.BuildingIPbyTypeGet(parameters.type, parameters.level, false)).Result);
            }

            return BadRequest("GetBuildingByType is invalid");       // 400 Error     
        }

        [HttpGet("UpdateIPRanking")]
        public IActionResult UpdateIPRanking()
        {
            BuildingManage buildingManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRanking()).Result);
            }

            return BadRequest("UpdateIPRanking is invalid");       // 400 Error     
        }

        [HttpGet("UpdateIPRankingByType")]
        public IActionResult UpdateIPRankingByType([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRankingByType(parameters.type, parameters.level)).Result);
            }

            return BadRequest("UpdateIPRankingByType is invalid");       // 400 Error     
        }

    }
}
