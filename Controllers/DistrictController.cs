using MetaverseMax.Database;
using MetaverseMax.ServiceClass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MetaverseMax.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistrictController : ControllerBase
    {       
        private readonly ILogger<DistrictController> _logger;
        private readonly MetaverseMaxDbContext _context;
        private DistrictDB districtDB;

        public DistrictController(MetaverseMaxDbContext context, ILogger<DistrictController> logger)
        {
            _logger = logger;
            _context = context;
            districtDB = new(_context);
        }

        [HttpGet("GetTaxChange")]
        public IActionResult GetTaxChange([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictWebMap districtWebMap = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetTaxChange(parameters.district_id));
            }
            return BadRequest("Get District Tax changes request is invalid");
        }

        [HttpGet("GetPerksAll")]
        public IActionResult GetPerksAll()
        {
            DistrictPerkManage districtPerkManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtPerkManage.GetPerks()).Result);
            }
            return BadRequest("Get District Perks All request is invalid");
        }

        [HttpGet]
        public IActionResult Get([FromQuery] QueryParametersDistrict parameters)
        {            
            if (ModelState.IsValid)
            {
                return Ok( GetDistrict(parameters.district_id) );
            }
            return BadRequest("District is invalid");       // 400 Error
        }

        [HttpGet("GetMCP")]
        public IActionResult GetMCP([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictManage districtManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtManage.GetDistrictMCP(parameters.district_id)).Result);
            }
            return BadRequest("District is invalid");       // 400 Error
        }

        [HttpGet("Get_All")]
        public IActionResult Get_All([FromQuery] QueryParametersDistrictGetAll parameters)
        {
            DistrictWebMap districtWebMap = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetDistrictAll(parameters.opened, parameters.includeTaxHistory));
            }
            return BadRequest("District.Get_All is invalid");       // 400 Error
        }

        [HttpGet("GetDistrictId_List")]
        public IActionResult GetDistrictId_List([FromQuery] QueryParametersDistrictGetOpened parameters)
        {
            DistrictWebMap districtWebMap = new(_context);
            if (ModelState.IsValid)
            {
                return Ok( districtWebMap.GetDistrictIdList(parameters.opened) );
            }
            return BadRequest("District list request is invalid");
        }

        [HttpGet("UpdateDistrict")]
        public IActionResult UpdateDistrict([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictWebMap districtWebMap = new(_context); 
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtWebMap.UpdateDistrict(parameters.district_id)).Result);
            }
            return BadRequest("District update action is invalid");
        }

        [HttpGet("UpdateAllOpenedDistricts")]
        public IActionResult UpdateAllOpenedDistricts(QueryParametersSecurity parametersSecurity)
        {
            DistrictManage districtManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(Task.Run(() => districtManage.UpdateAllDistricts(parametersSecurity.secure_token)).Result);
            }
            return BadRequest("District update action is invalid");
        }
     

        private DistrictWeb GetDistrict(int district_id)
        {
            DistrictWeb districtWeb = new();
            DistrictWebMap districtWebMap = new(_context);

            District district, districtHistory_1Mth = new();
            DistrictContent districtContent = new();
            bool getTaxHistory = true;

            string content = string.Empty;
            Common common = new();
            bool perksDetail = true;

            try
            {
                
                district = districtDB.DistrictGet(district_id);
                districtHistory_1Mth = districtDB.DistrictGet_History1Mth(district_id);

                if (district.district_id == 0)
                {

                    district.owner_name = "Unclaimed District";
                }
                else
                {
                    districtWeb = districtWebMap.MapData_DistrictWeb(district, districtHistory_1Mth, perksDetail, getTaxHistory);                    
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrict() : Error District_id: ", district_id.ToString()));
                    _context.LogEvent(log);
                }
            }

            return districtWeb;
        }

    }
}
