using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("GetWorldNames")]
        public IActionResult GetWorldNames()
        {
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => plotManage.GetWorldNames()).Result);
            }

            return BadRequest("GetWorldNames failed");       // 400 Error     
        }


        [HttpGet("GetPoiMCP")]
        public IActionResult GetPoiMCP([FromQuery] QueryParametersTokenID parameters)
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.GetPoiMCP(new List<int> { parameters.token_id })).Result);
            }

            return BadRequest("GetPoiMCP failed");       // 400 Error     
        }

        [HttpGet("Get_SyncHistory")]
        public IActionResult Get_SyncHistory()
        {
            _context.worldTypeSelected = common.IdentifyWorld(Request.Path);
            SyncHistoryDB syncHistoryDB = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => _context.syncHistory.ToArray()).Result);
            }

            return BadRequest("Get_SyncHistory failed");       // 400 Error     
        }

        [HttpGet("Get_WorldParcel")]
        public IActionResult Get_WorldParcel()
        {
            _context.worldTypeSelected = common.IdentifyWorld(Request.Path);
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => plotManage.GetParcel(0)).Result);
            }

            return BadRequest("Get_WorldParcel failed");       // 400 Error     
        }
        
        [HttpGet("Get_DistrictParcel")]
        public IActionResult Get_DistrictParcel([FromQuery] QueryParametersDistrictId parameters)
        {
            _context.worldTypeSelected = common.IdentifyWorld(Request.Path);
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => plotManage.GetParcel(parameters.district_id)).Result);
            }

            return BadRequest("Get_DistrictParcel failed");       // 400 Error     
        }

        [HttpGet("UpdatePlotSync")]
        public IActionResult UpdatePlotSync([FromQuery] QueryParametersPlotSync parameters)
        {
            SyncWorld syncWorld = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(syncWorld.SyncActiveDistrictPlot(parameters.secure_token, parameters.interval));
            }

            return BadRequest("Sync Failed");       // 400 Error     
        }

        [HttpGet("UpdatePlotSyncSingleWorld")]
        public IActionResult UpdatePlotSyncSingleWorld([FromQuery] QueryParametersPlotSync parameters)
        {
            SyncWorld syncWorld = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(syncWorld.UpdatePlotSyncSingleWorld(parameters.secure_token, parameters.interval));
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

            return BadRequest("AddUpdatePlotSingle failed");       // 400 Error     
        }

        [HttpGet("BuildingIPbyTypeGet")]
        public IActionResult BuildingIPbyTypeGet([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.BuildingIPbyTypeGet(parameters.type, parameters.level, false, 50, true, parameters.requester_matic)).Result);
            }

            return BadRequest("GetBuildingByType failed");       // 400 Error     
        }

        [HttpGet("OfficeGlobalSummary")]
        public IActionResult OfficeGlobalSummary()
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(buildingManage.OfficeGlobalSummaryGet());
            }

            return BadRequest("GetBuildingByType failed");       // 400 Error     
        }

        [HttpGet("UpdateIPRanking")]
        public IActionResult UpdateIPRanking()
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRanking()).Result);
            }

            return BadRequest("UpdateIPRanking failed");       // 400 Error     
        }

        [HttpGet("UpdateIPRankingByType")]
        public IActionResult UpdateIPRankingByType([FromQuery] QueryParametersTypeLevel parameters)
        {
            BuildingManage buildingManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.UpdateIPRankingByType(parameters.type, parameters.level, 100, true, parameters.requester_matic)).Result);
            }

            return BadRequest("UpdateIPRankingByType failed");       // 400 Error     
        }

       



        [HttpGet("UnitTest_GetSyncPlotList")]
        public IActionResult UnitTest_GetSyncPlotList()
        {
            PlotManage plotManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(plotManage.UnitTest_GetSyncPlotList());
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }

        [HttpGet("UnitTest_RoutePathTest")]
        public IActionResult UnitTest_RoutePathTest()
        {
            // should work with full range of routes : https://localhost:44360/api/bnb/plot/UnitTest_RoutePathTest , https://localhost:44360/api/eth/plot/UnitTest_RoutePathTest
            if (ModelState.IsValid)
            {
                return Ok(Request.Path.ToString().ToLower().Contains("/api/bnb/"));
            }

            return BadRequest("UnitTest_RoutePathTest is invalid");       // 400 Error   
        }

        [HttpGet("UnitTest_POIActive")]
        public IActionResult UnitTest_POIActive()
        {
            SyncWorld syncWorld = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(syncWorld.UnitTest_POIActive());
            }

            return BadRequest("UnitTest is invalid");       // 400 Error     
        }


    }
}
