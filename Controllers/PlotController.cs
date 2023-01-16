using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.Controllers
{   

    [ApiController]
    [Route("api/[controller]")]
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]
    public class PlotController : ControllerBase
    {
        private readonly ILogger<PlotController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private Common common = new();

        public PlotController(MetaverseMaxDbContext context, ILogger<PlotController> logger)        
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("GetPlotsMCP")]
        public IActionResult GetPlotsMCP([FromQuery] QueryParametersGetPlotMatric parameters)
        {
            //Example: BNB first 3 rows 1500plots / 13 mins https://localhost:44360/api/bnb/plot/GetPlotsMCP?secure_token=JUST_SIMPLE_CHECK123&interval=100&start_pos_x=0&start_pos_y=&end_pos_x=499&end_pos_y=3
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(plotManage.GetPlotsMCP(parameters));
            }

            return BadRequest("GetPlotsMCP Failed");       // 400 Error     
        }


        [HttpGet("UpdatePlotSync")]
        public IActionResult UpdatePlotSync([FromQuery] QueryParametersPlotSync parameters)
        {
            SyncWorld syncWorld = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(syncWorld.SyncActiveDistrictPlot( parameters.secure_token, parameters.interval) );
            }

            return BadRequest("Sync Failed");       // 400 Error     
        }

        [HttpGet("AddUpdatePlotSingle")]
        public IActionResult AddUpdatePlotSingle([FromQuery] QueryParametersPlotSingle parameters)
        {
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(plotManage.AddOrUpdatePlot(parameters.plot_id, parameters.posX, parameters.posY, true));
            }

            return BadRequest("AddUpdatePlotSingle is invalid");       // 400 Error     
        }

        [HttpGet("BuildingIPbyTypeGet")]
        public IActionResult BuildingIPbyTypeGet([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() =>  buildingManage.BuildingIPbyTypeGet(parameters.type, parameters.level, false)).Result);
            }

            return BadRequest("GetBuildingByType is invalid");       // 400 Error     
        }

        [HttpGet("UpdateIPRanking")]
        public IActionResult UpdateIPRanking()
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRanking()).Result);
            }

            return BadRequest("UpdateIPRanking is invalid");       // 400 Error     
        }

        [HttpGet("UpdateIPRankingByType")]
        public IActionResult UpdateIPRankingByType([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRankingByType(parameters.type, parameters.level)).Result);
            }

            return BadRequest("UpdateIPRankingByType is invalid");       // 400 Error     
        }

        [HttpGet("GetWorldNames")]
        public IActionResult GetWorldNames()
        {
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => plotManage.GetWorldNames()).Result);
            }

            return BadRequest("GetWorldNames is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_ActivePlotByDistrictDataSync")]
        public IActionResult UnitTest_ActivePlotByDistrictDataSync([FromQuery] QueryParametersDistrict parameters)
        {
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => plotManage.UnitTest_ActivePlotByDistrictDataSync(parameters.district_id)).Result);
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_RoutePathTest")]
        public IActionResult UnitTest_RoutePathTest()
        {
            // should would with full range of routes : https://localhost:44360/api/bnb/plot/UnitTest_RoutePathTest , https://localhost:44360/api/eth/plot/UnitTest_RoutePathTest
            if (ModelState.IsValid)
            {
                return Ok(Request.Path.ToString().ToLower().Contains("/api/bnb/"));
            }

            return BadRequest("UnitTest_RoutePathTest is invalid");       // 400 Error   
        }
    }
}
