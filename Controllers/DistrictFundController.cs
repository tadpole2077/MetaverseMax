﻿using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MetaverseMax.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]
    public class DistrictFundController : ControllerBase
    {
        private readonly ILogger<DistrictController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private Common common = new();

        public DistrictFundController(MetaverseMaxDbContext context, ILogger<DistrictController> logger)
        {
            _logger = logger;
            _context = context;

        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersFund parameters)
        {
            DistrictFundManage districtFundManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(districtFundManage.GetHistory(parameters.district_id, parameters.daysHistory));
            }
            return BadRequest("Get District Fund history request is invalid");
        }

        [HttpGet("UpdateAllDistrictsFund")]
        public IActionResult UpdateAllDistrictsFund([FromQuery] QueryParametersSecurity parameters)
        {
            DistrictFundManage districtFundManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtFundManage.UpdateFundAll(parameters)).Result);
            }
            return BadRequest("All District Fund update request is invalid");
        }

        [HttpGet("UpdateTaxHistoryChange")]
        public IActionResult UpdateTaxHistoryChange()
        {
            DistrictFundManage districtFundManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(districtFundManage.UpdateTaxChanges());
            }
            return BadRequest("Update Tax History change request is invalid");
        }
    }
}
