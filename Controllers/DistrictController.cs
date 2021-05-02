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
    // Model Binding Class for Controller Parameters
    public class QueryParametersDistrict
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int district_id { get; set; }

    }
    public class QueryParametersDistrictGetOpened
    {
        [BindRequired]
        public bool opened { get; set; }

    }

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
            if (ModelState.IsValid)
            {
                return Ok(GetDistrictAll(parameters.opened));
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
            if (ModelState.IsValid)
            {
                return Ok(UpdateDistrict(parameters.district_id));
            }
            return BadRequest("District update action is invalid");
        }

        [HttpGet("UpdateAllOpenedDistricts")]
        public IActionResult UpdateAllOpenedDistricts()
        {
            if (ModelState.IsValid)
            {
                return Ok( UpdateAllDistricts() );
            }
            return BadRequest("District update action is invalid");
        }


        // Private Methods - BL
        private IEnumerable<DistrictWeb> GetDistrictAll(bool isOpened)
        {
            List<District> districtList = new();
            List<DistrictWeb> districtWebList = new();

            try
            {
                districtList = districtDB.DistrictGetAll(isOpened).ToList();
                foreach(District district in districtList)
                {
                    districtWebList.Add(MapData_DistrictWeb(district));
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrictAll() : Error "));
                    _context.LogEvent(log);
                }
            }

            return districtWebList.ToArray();
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

        private IEnumerable<DistrictName> GetDistrictsFromMCP(bool isOpened)
        {
            List<DistrictName> districtList = new();
            string content = string.Empty;
            try
            {
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

                if (content.Length == 0)
                {
                    districtList.Add(new DistrictName { district_id = 0, district_name = "Loading Issue" });
                }
                else
                {
                    JObject jsonContent = JObject.Parse(content);
                    JArray districts = jsonContent.Value<JArray>("stat");
                    if (districts != null && districts.HasValues)
                    {
                        for(int index = 0; index < districts.Count; index++)
                        {
                            JToken districtToken = districts[index];
                            districtList.Add(new DistrictName()
                            {
                                district_id = districtToken.Value<int?>("region_id") ?? 0,
                                district_name = ""
                            });
                        }                        
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("GetDistrictsFromMCP() : Error "));
                    _context.LogEvent(log);
                }
            }
            

            return districtList.ToArray();
        }

        private int UpdateAllDistricts()
        {
            string content = string.Empty;
            int districtUpdateCount = 0;
            try
            {
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
                            JToken districtToken = districts[index];
                            UpdateDistrictByToken(districtToken);

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

        private DistrictWeb GetDistrict(int district_id)
        {
            DistrictWeb districtWeb = new();
            District district = new();
            DistrictContent districtContent = new();
            Citizen citizen = new();
            string content = string.Empty;
            Common common = new();

            try
            {
                

                district = districtDB.DistrictGet(district_id);

                if (district.district_id == 0)
                {
                    district.owner_name = "Unclaimed District";
                }
                else
                {
                    districtWeb = MapData_DistrictWeb(district);                    
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
                        district.owner_matic = districtToken.Value<string>("buyer") ?? "Not Found";

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

        private bool UpdateDistrict(int district_id)
        {
            DistrictDB districtDB = new(_context);
            District district = new();
            Citizen citizen = new();
            string content = string.Empty;
            Common common = new();
            bool returnCode = false;

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
                        returnCode = UpdateDistrictByToken( districtData[0] );
                    }
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

            return returnCode;
        }

        private bool UpdateDistrictByToken(JToken districtToken)
        {
            DistrictDB districtDB = new(_context);
            District district = new();            
            Common common = new();

            try
            {
                district.district_id = districtToken.Value<int?>("region_id") ?? 0;
                district.district_matic_key = districtToken.Value<string>("address");

                district.owner_name = districtToken.Value<string>("owner_nickname") ?? "Not Found";
                district.owner_avatar_id = districtToken.Value<int?>("owner_avatar_id") ?? 0;
                district.owner_matic = districtToken.Value<string>("buyer") ?? "Not Found";

                district.active_from = common.TimeFormatStandardDT(districtToken.Value<string>("active_from") ?? "", null);
                district.plots_claimed = districtToken.Value<int?>("claimed_cnt") ?? 0;
                district.building_count = districtToken.Value<int?>("buildings_cnt") ?? 0;
                district.land_count = districtToken.Value<int?>("lands") ?? 0;

                district.energy_tax = districtToken.Value<int?>("energy_tax") ?? 0;
                district.production_tax = districtToken.Value<int?>("production_tax") ?? 0;
                district.commercial_tax = districtToken.Value<int?>("commercial_tax") ?? 0;
                district.citizen_tax = districtToken.Value<int?>("citizen_tax") ?? 0;

                JToken constructionTax = districtToken.Value<JToken>("construction_taxes");
                if (constructionTax != null && constructionTax.HasValues)
                {
                    district.construction_energy_tax = constructionTax.Value<int?>(0) ?? 0;
                    district.construction_industry_production_tax = constructionTax.Value<int?>(1) ?? 0;
                    district.construction_residential_tax = constructionTax.Value<int?>(2) ?? 0;
                    district.construction_commercial_tax = constructionTax.Value<int?>(3) ?? 0;
                    district.construction_municipal_tax = constructionTax.Value<int?>(4) ?? 0;
                }

                district.distribution_period = districtToken.Value<int?>("distribution_period") ?? 0;
                district.insurance_commission = districtToken.Value<int?>("insurance_commission") ?? 0;

                district.resource_zone = districtToken.Value<int?>("resources") ?? 0;

                districtDB.DistrictUpdate(district);

            }
            catch (Exception ex)
            {
                string log = ex.Message;
                if (_context != null)
                {
                    _context.LogEvent(String.Concat("UpdateDistrictByToken() : Error District_id: ", district.district_id.ToString()));
                    _context.LogEvent(log);
                }
            }

            return true;
        }

        private DistrictWeb MapData_DistrictWeb(District district)
        {
            DistrictWeb districtWeb = new();
            Citizen citizen = new();
            Common common = new();
            DistrictContent districtContent = new();


            districtWeb.update_instance = district.update_instance;
            districtWeb.district_id = district.district_id;
            districtWeb.owner_name = district.owner_name ?? "Not Found";
            districtWeb.owner_avatar_id = district.owner_avatar_id ?? 0;
            districtWeb.owner_url = citizen.AssignDefaultOwnerImg(district.owner_avatar_id.ToString() ?? "0");
            districtWeb.owner_matic = district.owner_matic ?? "Not Found";

            districtWeb.active_from = common.TimeFormatStandard("", district.active_from);
            districtWeb.plots_claimed = district.plots_claimed;
            districtWeb.building_count = district.building_count;
            districtWeb.land_count = district.land_count;

            districtWeb.energy_count = district.energy_plot_count ?? 0;
            districtWeb.industry_count = district.industry_plot_count ?? 0;
            districtWeb.production_count = district.production_plot_count ?? 0;
            districtWeb.office_count = district.office_plot_count ?? 0;
            districtWeb.commercial_count = district.commercial_plot_count ?? 0;
            districtWeb.residential_count = district.residential_plot_count ?? 0;
            districtWeb.municipal_count = district.municipal_plot_count ?? 0;
            districtWeb.poi_count = district.poi_plot_count ?? 0;

            districtWeb.construction_energy_tax = district.construction_energy_tax ?? 0;
            districtWeb.construction_industry_production_tax = district.construction_industry_production_tax ?? 0;
            districtWeb.construction_commercial_tax = district.construction_commercial_tax ?? 0;            
            districtWeb.construction_municipal_tax = district.construction_municipal_tax ?? 0;            
            districtWeb.construction_residential_tax = district.construction_residential_tax ?? 0;


            districtWeb.energy_tax = district.energy_tax ?? 0;
            districtWeb.production_tax = district.production_tax ?? 0;
            districtWeb.commercial_tax = district.commercial_tax ?? 0;
            districtWeb.citizen_tax = district.citizen_tax ?? 0;

            districtContent = districtDB.DistrictContentGet(district.district_id);

            if (districtContent != null)
            {
                districtWeb.district_name = districtContent.district_name;
                districtWeb.promotion = districtContent.district_promotion;
                districtWeb.promotion_start = common.TimeFormatStandard("", districtContent.district_promotion_start);
                districtWeb.promotion_end = common.TimeFormatStandard("", districtContent.district_promotion_end);
            }
            return districtWeb;
        }
}
}
