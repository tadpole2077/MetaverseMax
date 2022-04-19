using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetHistoryController : ControllerBase
    {
        private readonly ILogger<AssetHistoryController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private Common common = new();

        public AssetHistoryController(MetaverseMaxDbContext context, ILogger<AssetHistoryController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]        
        public IActionResult Get([FromQuery] QueryParametersTokenID_IPEfficiency parameters)
        {
            BuildingManage buildingManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => buildingManage.GetHistory(parameters.token_id, parameters.ip_efficiency, parameters.ip_efficiency_bonus_bug) ).Result);
            }
            return BadRequest("Get Asset History request is invalid");
        }

        
        [HttpGet("GetCitizenHistory")]
        public IActionResult GetCitizenHistory([FromQuery] QueryParametersCitizenHistory parameters)
        {
            CitizenManage citizenManage = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(citizenManage.GetCitizenHistory(parameters.token_id, parameters.production_date));
            }

            return BadRequest("Get Citizen History Call is invalid");       // 400 Error   
        }

        [HttpGet("WaitPeriod")]
        public IActionResult WaitPeriod()
        {
            CitizenManage citizenManage = new(_context);

            if (ModelState.IsValid)
            {
                Task.Run(async () => { await WaitPeriodAction(); }).Wait();
                return Ok();
            }

            return BadRequest("Unable to Wait");       // 400 Error   
        }

        private async Task WaitPeriodAction() {
            await Task.Delay(10000); //.Wait(); //1000 = 1sec
            return;
        }
    }
}
