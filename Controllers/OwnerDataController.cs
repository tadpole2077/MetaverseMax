using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetaverseMax.ServiceClass;

namespace MetaverseMax.Controllers
{
    //[Route("[controller]")]
    [ApiController]
    [Route("api/[controller]")]
    public class OwnerDataController : ControllerBase
    {
        private readonly ILogger<OwnerDataController> _logger;

        private OwnerData ownerData = new();
        private Common common = new();

        public OwnerDataController(ILogger<OwnerDataController> logger)
        {
            _logger = logger;
        }
        //[ActionName("FOO")]   only used if call is api/owenerdata/foo
        //[Route("api/ownerdata/")]
        //[HttpGet("{plotX}/{plotY}")]
        //[Route("ownerdata")]
        //[HttpGet]
        [HttpGet]
        public OwnerData Get(string plotX, string plotY)
        {
            int returnCode = GetFromLandCoord(Convert.ToInt32(plotX), Convert.ToInt32(plotY));
            if (returnCode != -1)
            {
                _ = GetOwnerLands();
            }
            return ownerData;
        }

        [HttpGet("GetUsingMatic")]       
        public OwnerData GetUsingMatic(string owner_matic_key)
        {
            int returnCode = GetFromMaticKey(owner_matic_key);
            if (returnCode != -1)
            {
                _ = GetOwnerLands();
            }

            return ownerData;
        }

        private int GetOwnerLands()
        {
            string content = string.Empty;
            byte[] byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerData.owner_matic_key + "\",\"short\": false}");
            Building building = new();
            Citizen citizen = new();

            WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/user/assets/lands");
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
            JObject jsonContent = JObject.Parse(content);
            JArray lands = jsonContent["items"] as JArray;
            if (lands.Count >0)
            {
                JToken land = lands.Children().First();

                ownerData.owner_matic_key = land.Value<string>("owner") ?? "Not Found";
                ownerData.plot_count = lands.Count;

                if (lands.Any())
                {
                    ownerData.owner_land = lands.Select(landInstance => new OwnerLand
                    {
                        district_id = landInstance.Value<int?>("region_id") ?? 0,
                        pos_x = landInstance.Value<int?>("x") ?? 0,
                        pos_y = landInstance.Value<int?>("y") ?? 0,
                        plot_ip = landInstance.Value<int?>("influence") ?? 0,
                        ip_bonus = (landInstance.Value<int?>("influence") ?? 0) * (landInstance.Value<int?>("influence_bonus") ?? 0) / 100,
                        building_type = landInstance.Value<int?>("building_type_id") ?? 0,
                        building_desc = building.BuildingType(landInstance.Value<int?>("building_type_id") ?? 0, landInstance.Value<int?>("building_id") ?? 0),
                        building_img = building.BuildingImg(landInstance.Value<int?>("building_type_id") ?? 0, landInstance.Value<int?>("building_id") ?? 0, landInstance.Value<int?>("building_level") ?? 0),
                        last_actionUx = landInstance.Value<double?>("last_action") ?? 0,
                        last_action = common.UnixTimeStampToDateTime(landInstance.Value<double?>("last_action"), "Empty Plot"),
                        token_id = landInstance.Value<int?>("token_id") ?? 0,
                        building_level = landInstance.Value<int?>("building_level") ?? 0,
                        citizen_count = citizen.GetCitizenCount(landInstance.Value<JArray>("citizens")),
                        citizen_url = citizen.GetCitizenUrl(landInstance.Value<JArray>("citizens")),
                        citizen_stamina = citizen.GetLowStamina(landInstance.Value<JArray>("citizens")),
                        citizen_stamina_alert = citizen.CheckCitizenStamina(landInstance.Value<JArray>("citizens"), landInstance.Value<int?>("building_type_id") ?? 0),                      
                        forsale_price = building.GetSalePrice(landInstance.Value<JToken>("sale_data"))                        
                    })
                        .OrderBy(row => row.district_id).ThenBy(row => row.pos_x).ThenBy(row => row.pos_y);


                    ownerData.developed_plots = ownerData.owner_land.Where(
                        row => row.last_action != "Empty Plot"
                        ).Count();

                    // Get Last Action across all lands for target player
                    ownerData.last_action = string.Concat(common.UnixTimeStampToDateTime(ownerData.owner_land.Max(row => row.last_actionUx), "No Lands"), " GMT");

                    ownerData.plots_for_sale = ownerData.owner_land.Where(
                        row => row.forsale_price > 0
                        ).Count();

                    ownerData.district_plots = building.DistrictPlots(ownerData.owner_land);
                }
            }
            else
            {
                if ( string.Equals(ownerData.owner_matic_key, "Owner not Found") )
                {
                    ownerData.last_action = "Empty Plot, It could be Yours today!";
                }
                else
                {
                    ownerData.last_action = "This player owns no land plots in Tron World";
                }
            }

            return 0;
        }


        private int GetFromMaticKey(string ownerMaticKey)
        {
            String content = string.Empty;
            Citizen citizen = new();
            byte[] byteArray;
            WebRequest request;
            Stream dataStream;
            int returnCode = 0;

            try
            {
                //ownerData.wallet_public = WalletConvert(jsonContent.Value<string>("owner") ?? string.Empty);
                if (string.IsNullOrEmpty(ownerMaticKey) || ownerMaticKey.Equals("Not Found"))
                {
                    AssignUnknownOwner();
                    returnCode = -1;
                }
                else
                {
                    // POST from User/Get REST WS
                    byteArray = Encoding.ASCII.GetBytes("{\"address\": \"" + ownerMaticKey + "\",\"dapper\": false,\"sign\": null }");
                    request = WebRequest.Create("https://ws-tron.mcp3d.com/user/get");
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    dataStream = request.GetRequestStream();
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
                        ownerData.owner_name = string.Concat("Owner not Found matching Matic key ", ownerMaticKey);
                        returnCode = -1;
                    }
                    else{
                        ownerData.owner_matic_key = ownerMaticKey;
                        JObject jsonContent = JObject.Parse(content);

                        ownerData.owner_name = jsonContent.Value<string>("avatar_name") ?? "Not Found";
                        ownerData.owner_url = citizen.AssignDefaultOwnerImg(jsonContent.Value<string>("avatar_id") ?? "");

                        ownerData.registered_date = common.TimeFormatStandard(jsonContent.Value<string>("registered"), null);
                        ownerData.last_visit = common.TimeFormatStandard(jsonContent.Value<string>("last_visited"), null);
                    }
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return returnCode;
        }

        private int AssignUnknownOwner()
        {
            Citizen citizen = new();
            ownerData.owner_matic_key = "Owner not Found";
            ownerData.last_action = "Empty Plot, It could be Yours today!";
            ownerData.owner_url = citizen.AssignDefaultOwnerImg("0");

            return 0;
        }

        private int GetFromLandCoord(int posX, int posY)
        {
            String content = string.Empty;
            Citizen citizen = new();
            int returnCode = 0;

            try
            {
                // POST from Land/Get REST WS
                byte[] byteArray = Encoding.ASCII.GetBytes("{\"x\": \""+posX+"\",\"y\": \""+posY+"\"}");
                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/land/get");
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
                    ownerData.owner_name = "Plot does not exist";
                    returnCode = -1;
                }
                else {
                    JObject jsonContent = JObject.Parse(content);
                    ownerData.owner_matic_key = jsonContent.Value<string>("owner") ?? "Not Found";
                    GetFromMaticKey(ownerData.owner_matic_key); 
                }
            }
            catch (Exception ex)
            {
                string log = ex.Message;
            }

            return returnCode;
        }       

    }
}
