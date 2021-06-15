using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public class DistrictFundController : ControllerBase
    {
        private readonly ILogger<DistrictController> _logger;
        private readonly MetaverseMaxDbContext _context;

        public DistrictFundController(MetaverseMaxDbContext context, ILogger<DistrictController> logger)
        {
            _logger = logger;
            _context = context;

        }

        [HttpGet("UpdateAllDistrictsFund")]
        public IActionResult UpdateAllDistrictsFund([FromQuery] QueryParametersSecurity parameters)
        {
            DistrictFundManage districtFundManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(districtFundManage.UpdateFundAll(parameters) );
            }
            return BadRequest("All District Fund update request is invalid");
        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersFund parameters)
        {
            DistrictFundManage districtFundManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(districtFundManage.GetHistory(parameters.district_id, parameters.daysHistory));
            }
            return BadRequest("Get District Fund history request is invalid");
        }
    }
}
