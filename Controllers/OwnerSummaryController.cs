using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OwnerSummaryController : Controller
    {
        private readonly ILogger<OwnerSummaryController> _logger;
        private readonly MetaverseMaxDbContext _context;

        private Common common = new();

        public OwnerSummaryController(MetaverseMaxDbContext context, ILogger<OwnerSummaryController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersDistrictOwner parameters )
        {
            OwnerSummaryDistrictDB ownerSummaryDistrictDB = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(ownerSummaryDistrictDB.GetOwnerSummeryDistrict(parameters.district_id, parameters.update_instance));
            }
            return BadRequest("District is invalid");       // 400 Error            
        }
    }
}
