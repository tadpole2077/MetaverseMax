using MetaverseMax.Database;
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
    public class DistrictController : ControllerBase
    {
        private readonly ILogger<DistrictController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private DistrictDB districtDB;
        private Common common = new();

        public DistrictController(MetaverseMaxDbContext context, ILogger<DistrictController> logger)
        {
            _logger = logger;
            _context = context;
            districtDB = new(_context);
        }

        [HttpGet("GetTaxChange")]
        public IActionResult GetTaxChange([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictWebMap districtWebMap = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetTaxChange(parameters.district_id));
            }
            return BadRequest("Get District Tax changes request is invalid");
        }

        [HttpGet("GetPerksAll")]
        public IActionResult GetPerksAll()
        {
            DistrictPerkManage districtPerkManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtPerkManage.GetPerks()).Result);
            }
            return BadRequest("Get District Perks All request is invalid");
        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictManage districtManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(districtManage.GetDistrict(parameters.district_id));
            }
            return BadRequest("District is invalid");       // 400 Error
        }

        [HttpGet("GetMCP")]
        public IActionResult GetMCP([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictManage districtManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtManage.GetDistrictMCP(parameters.district_id)).Result);
            }
            return BadRequest("District is invalid");       // 400 Error
        }

        [HttpGet("Get_All")]
        public IActionResult Get_All([FromQuery] QueryParametersDistrictGetAll parameters)
        {
            DistrictWebMap districtWebMap = new(_context, common.IdentifyWorld(Request.Path));

            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetDistrictAll(parameters.opened, parameters.includeTaxHistory));
            }
            return BadRequest("District.Get_All is invalid");       // 400 Error
        }

        [HttpGet("GetDistrictId_List")]
        public IActionResult GetDistrictId_List([FromQuery] QueryParametersDistrictGetOpened parameters)
        {
            DistrictWebMap districtWebMap = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetDistrictIdList(parameters.opened));
            }
            return BadRequest("District list request is invalid");
        }

        [HttpGet("UpdateDistrict")]
        public IActionResult UpdateDistrict([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictManage districtManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtManage.UpdateDistrict(parameters.district_id)).Result);
            }
            return BadRequest("District update action is invalid");
        }

        [HttpGet("UpdateAllOpenedDistricts")]
        public IActionResult UpdateAllOpenedDistricts([FromQuery] QueryParametersSecurity parametersSecurity)
        {
            DistrictManage districtManage = new(_context, common.IdentifyWorld(Request.Path));
            if (ModelState.IsValid)
            {
                districtManage.ArchiveOwnerSummaryDistrict();
                return Ok(Task.Run(() => districtManage.UpdateAllDistricts(parametersSecurity.secure_token)).Result);
            }
            return BadRequest("District update action is invalid");
        }

    }
}
