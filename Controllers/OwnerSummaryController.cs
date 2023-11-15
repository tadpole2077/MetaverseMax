using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/trx/[controller]")]
    [Route("api/bnb/[controller]")]
    [Route("api/eth/[controller]")]
    public class OwnerSummaryController : Controller
    {
        private readonly ILogger<OwnerSummaryController> _logger;
        private readonly MetaverseMaxDbContext _context;

        private readonly ServiceCommon common = new();

        public OwnerSummaryController(MetaverseMaxDbContext context, ILogger<OwnerSummaryController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersDistrictOwner parameters)
        {
            OwnerManage ownerManage = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(ownerManage.GetOwnerSummaryDistrict(parameters.district_id, parameters.update_instance));
            }
            return BadRequest("Request is invalid");       // 400 Error            
        }
    }
}
