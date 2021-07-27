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

        [HttpGet("GetPerksAll")]
        public IActionResult GetPerksAll()
        {
            DistrictPerkManage districtPerkManage = new(_context);
            if (ModelState.IsValid)
            {
                return Ok(districtPerkManage.GetPerks());
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
            if (ModelState.IsValid)
            {
                return Ok(GetDistrictMCP(parameters.district_id));
            }
            return BadRequest("District is invalid");       // 400 Error
        }

        [HttpGet("Get_All")]
        public IActionResult Get_All([FromQuery] QueryParametersDistrictGetOpened parameters)
        {
            DistrictWebMap districtWebMap = new(_context);

            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.GetDistrictAll(parameters.opened));
            }
            return BadRequest("District.Get_All is invalid");       // 400 Error
        }

        [HttpGet("GetDistrictId_List")]
        public IActionResult GetDistrictId_List([FromQuery] QueryParametersDistrictGetOpened parameters)
        {     
            if (ModelState.IsValid)
            {
                return Ok( GetDistrictIdList(parameters.opened) );
            }
            return BadRequest("District list request is invalid");
        }

        [HttpGet("UpdateDistrict")]
        public IActionResult UpdateDistrict([FromQuery] QueryParametersDistrict parameters)
        {
            DistrictWebMap districtWebMap = new(_context); 
            if (ModelState.IsValid)
            {
                return Ok(districtWebMap.UpdateDistrict(parameters.district_id));
            }
            return BadRequest("District update action is invalid");
        }

        [HttpGet("UpdateAllOpenedDistricts")]
        public IActionResult UpdateAllOpenedDistricts(QueryParametersSecurity parametersSecurity)
        {
            if (ModelState.IsValid)
            {
                return Ok( UpdateAllDistricts(parametersSecurity.secure_token) );
            }
            return BadRequest("District update action is invalid");
        }



        private IEnumerable<int> GetDistrictIdList(bool isOpened)
        {
            List<int> districtIDList = new();

            try
            {                
                districtIDList = districtDB.DistrictId_GetList().ToList();

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrictIDList() : Error "));
                    _context.LogEvent(log);
                }
            }

            return districtIDList.ToArray();
        }     

        private DistrictWeb GetDistrict(int district_id)
        {
            DistrictWeb districtWeb = new();
            DistrictWebMap districtWebMap = new(_context);

            District district, districtHistory_1Mth = new();
            DistrictContent districtContent = new();
            Citizen citizen = new();
            
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
                    districtWeb = districtWebMap.MapData_DistrictWeb(district, districtHistory_1Mth, perksDetail);                    
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

        private DistrictWeb GetDistrictMCP(int district_id)
        {
            DistrictWeb district = new();
            Citizen citizen = new();
            string content = string.Empty;
            Common common = new();

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"region_id\": " + district_id.ToString() + "}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/regions/list");
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Ensure correct dispose of WebRespose IDisposable class even if exception
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader reader = new(response.GetResponseStream());
                    content = reader.ReadToEnd();
                }
                if (content.Length == 0)
                {
                    district.owner_name = "Unclaimed District";
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districtData = jsonContent.Value<JArray>("stat");
                    if (districtData != null && districtData.HasValues)
                    {
                        JToken districtToken = districtData[0];
                        district.district_id = districtToken.Value<int?>("region_id") ?? 0;
                        district.owner_name = districtToken.Value<string>("owner_nickname") ?? "Not Found";
                        district.owner_url = citizen.AssignDefaultOwnerImg(districtToken.Value<string>("owner_avatar_id") ?? "");
                        district.owner_matic = districtToken.Value<string>("address") ?? "Not Found";

                        district.active_from = common.TimeFormatStandard(districtToken.Value<string>("active_from") ?? "", null);
                        district.plots_claimed = districtToken.Value<int?>("claimed_cnt") ?? 0;
                        district.building_count = districtToken.Value<int?>("buildings_cnt") ?? 0;
                        district.land_count = districtToken.Value<int?>("lands") ?? 0;
                        district.energy_tax = districtToken.Value<int?>("energy_tax") ?? 0;
                        district.production_tax = districtToken.Value<int?>("production_tax") ?? 0;
                        district.commercial_tax = districtToken.Value<int?>("commercial_tax") ?? 0;
                        district.citizen_tax = districtToken.Value<int?>("citizens_tax") ?? 0;
                    }
                    
                }            
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrict() : Error District_id: ", district_id.ToString() ));
                    _context.LogEvent(log);
                }
            }

            return district;
        }

        // Update All active districts from MCP REST WS, update owner summary per district using local db plot data
        private int UpdateAllDistricts(string secureToken)
        {
            DistrictDB districtDB;
            JToken districtToken;

            string content = string.Empty;
            int districtUpdateCount = 0;
            try
            {
                // As this service could be abused as a DDOS a security token is needed.
                if (!secureToken.Equals("JUST_SIMPLE_CHECK123"))
                {
                    return districtUpdateCount;
                    ;
                }

                districtDB = new DistrictDB(_context);

                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/regions/list");
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Ensure correct dispose of WebRespose IDisposable class even if exception
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader reader = new(response.GetResponseStream());
                    content = reader.ReadToEnd();
                }

                if (content.Length > 0)
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districts = jsonContent.Value<JArray>("stat");
                    if (districts != null && districts.HasValues)
                    {
                        for (int index = 0; index < districts.Count; index++)
                        {
                            districtToken = districts[index];
                            districtDB.UpdateDistrictByToken(districtToken);

                            districtUpdateCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("UpdateAllOpenedDistricts() : Error "));
                    _context.LogEvent(log);
                }
            }


            return districtUpdateCount;
        }
    }
}
