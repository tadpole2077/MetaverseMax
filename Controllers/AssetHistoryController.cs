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

        public AssetHistoryController(ILogger<AssetHistoryController> logger)
        {
            _logger = logger;
        }

        [HttpGet] 
        public BuildingHistory Get([FromQuery] string asset_id)
        {
            string content = string.Empty;
            string currectOwner = string.Empty;
            int buildingType, runInstanceCount = 0;
            byte[] byteArray = Encoding.ASCII.GetBytes("{\"token_id\": " + asset_id + ",\"token_type\": 1}");
            List<HistoryProduction> historyProductionList = new();
            List<ResourceTotal> resourceTotal = new();
            ResourceTotal currentResource = null; 
            BuildingHistory buildingHistory = new();
            Common common = new();

            try
            {

                WebRequest request = WebRequest.Create("https://ws-tron.mcp3d.com/user/assets/history");
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

                JArray historyItems = JArray.Parse(content);
                if (historyItems != null && historyItems.Count > 0)
                {
                    for (int index = 0; index < historyItems.Count; index++)
                    {
                        JToken historyItem = historyItems[index];

                        if (currectOwner == string.Empty)
                        {
                            currectOwner = historyItem.Value<string>("to_address") ?? "";
                        }

                        if ((historyItem.Value<string>("type") ?? "").Equals("management/resource_produced") && 
                             (historyItem.Value<string>("to_address") ?? "").Equals(currectOwner))
                        {
                            HistoryProduction historyProductionDetails = new();
                            historyProductionDetails.run_datetime = common.TimeFormatStandard(historyItem.Value<string>("event_time") ?? "");                            

                            runInstanceCount++;

                            JToken historyData = historyItem.Value<JToken>("data");
                            if (historyData != null && historyData.HasValues)
                            {
                                historyProductionDetails.amount_produced = historyData.Value<int?>("amount") ?? 0;
                                historyProductionDetails.buildingProduct = GetResourceName(historyData.Value<int?>("resourceId") ?? 0);

                                // Find Stored Resource match, and increment
                                if (resourceTotal.Count > 0)
                                {
                                    currentResource = (ResourceTotal)resourceTotal.Where(row => row.resourceId == (historyData.Value<int?>("resourceId") ?? 0)).FirstOrDefault();
                                }
                                if (currentResource == null)
                                {
                                    currentResource = new();
                                    currentResource.resourceId = historyData.Value<int?>("resourceId") ?? 0;
                                    currentResource.resouceName = historyProductionDetails.buildingProduct;
                                    resourceTotal.Add(currentResource);
                                }
                                                                
                                currentResource.resourceTotal += historyProductionDetails.amount_produced;

                                JToken historyLand = historyData.Value<JToken>("land");
                                if (historyLand != null && historyLand.HasValues)
                                {
                                    buildingType = historyLand.Value<int?>("building_type_id") ?? 0;

                                    historyProductionDetails.efficiency = CalculateEffiency(buildingType, historyLand.Value<int?>("building_level") ?? 0, historyProductionDetails.amount_produced, historyData.Value<int?>("resourceId") ?? 0);
                                }
                            }

                            historyProductionList.Add(historyProductionDetails);
                        }
                    }
                   
                    if (historyProductionList.Count > 0)
                    {                        
                        buildingHistory.runCount = runInstanceCount;
                        buildingHistory.startProduction = historyProductionList.Last().run_datetime;
                        buildingHistory.totalProduced = GetResourceTotalDisplay(resourceTotal);
                        buildingHistory.detail = historyProductionList.ToArray<HistoryProduction>();
                    }
                }
            }
            catch(Exception ex)
            {
                string log = ex.Message;
            }

            return buildingHistory;
        }

        public static IEnumerable<string> GetResourceTotalDisplay(List<ResourceTotal> resourceTotal)
        {
            List<string> formatedTotal = new();

            for(int count=0; count < resourceTotal.Count; count++){
                formatedTotal.Add (string.Concat("[", resourceTotal[count].resouceName, "]", resourceTotal[count].resourceTotal.ToString()));
            }

            return formatedTotal;
        }

        private static string GetResourceName(int buildingProduct)
        {
            return buildingProduct switch
            {
                (int)BUILDING_PRODUCT.WOOD => "Wood",
                (int)BUILDING_PRODUCT.SAND => "Sand",
                (int)BUILDING_PRODUCT.METAL => "Metal",
                (int)BUILDING_PRODUCT.BRICK => "Brick",
                (int)BUILDING_PRODUCT.GLASS => "Glass",
                (int)BUILDING_PRODUCT.CONCRETE => "Concrete",
                (int)BUILDING_PRODUCT.STONE => "Stone",
                (int)BUILDING_PRODUCT.STEEL => "Steel",
                (int)BUILDING_PRODUCT.WATER => "Water",
                (int)BUILDING_PRODUCT.ENERGY => "Energy",
                _ => "Product"
            };
        }

        private static int CalculateEffiency(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
        {
            int Effiency = 0;

            switch (buildingType)
            {
                case (int)BUILDINGTYPE_ENUM.RESIDENTIAL:
                    Effiency = 0;
                    break;
                case (int)BUILDINGTYPE_ENUM.ENERGY:
                    if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 750,
                            2 => (amount_produced * 100) / 836,
                            3 => (amount_produced * 100) / 938,
                            4 => (amount_produced * 100) / 1071,
                            5 => (amount_produced * 100) / 1286,
                            6 => (amount_produced * 100) / 2143,
                            7 => (amount_produced * 100) / 3321,
                            _=> 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 5000,
                            2 => (amount_produced * 100) / 5571,
                            3 => (amount_produced * 100) / 6250,
                            4 => (amount_produced * 100) / 7143,
                            5 => (amount_produced * 100) / 8571,
                            6 => (amount_produced * 100) / 14286,
                            7 => (amount_produced * 100) / 22143,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDINGTYPE_ENUM.COMMERCIAL:
                    Effiency = 0;
                    break;
                case (int)BUILDINGTYPE_ENUM.INDUSTRIAL:
                    if (buildingProduct == (int)BUILDING_PRODUCT.METAL)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 160,
                            2 => (amount_produced * 100) / 178,
                            3 => (amount_produced * 100) / 200,
                            4 => (amount_produced * 100) / 229,
                            5 => (amount_produced * 100) / 274,
                            6 => (amount_produced * 100) / 457,
                            7 => (amount_produced * 100) / 594,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.WOOD)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 375,
                            2 => (amount_produced * 100) / 418,
                            3 => (amount_produced * 100) / 469,
                            4 => (amount_produced * 100) / 536,
                            5 => (amount_produced * 100) / 643,
                            6 => (amount_produced * 100) / 1071,
                            7 => (amount_produced * 100) / 1393,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.SAND)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 225,
                            2 => (amount_produced * 100) / 251,
                            3 => (amount_produced * 100) / 281,
                            4 => (amount_produced * 100) / 321,
                            5 => (amount_produced * 100) / 386,
                            6 => (amount_produced * 100) / 643,
                            7 => (amount_produced * 100) / 836,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STONE)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 100,
                            2 => (amount_produced * 100) / 111,
                            3 => (amount_produced * 100) / 125,
                            4 => (amount_produced * 100) / 143,
                            5 => (amount_produced * 100) / 171,
                            6 => (amount_produced * 100) / 286,
                            7 => (amount_produced * 100) / 371,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDINGTYPE_ENUM.OFFICE:
                    Effiency = 0;
                    break;
                case (int)BUILDINGTYPE_ENUM.PRODUCTION:
                    if (buildingProduct == (int)BUILDING_PRODUCT.BRICK)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 40,
                            2 => (amount_produced * 100) / 45,
                            3 => (amount_produced * 100) / 50,
                            4 => (amount_produced * 100) / 57,
                            5 => (amount_produced * 100) / 69,
                            6 => (amount_produced * 100) / 114,
                            7 => (amount_produced * 100) / 177,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.GLASS)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 32,
                            2 => (amount_produced * 100) / 36,
                            3 => (amount_produced * 100) / 40,
                            4 => (amount_produced * 100) / 46,
                            5 => (amount_produced * 100) / 55,
                            6 => (amount_produced * 100) / 91,
                            7 => (amount_produced * 100) / 142,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE)
                    {
                        Effiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100) / 8,
                            2 => (amount_produced * 100) / 9,
                            3 => (amount_produced * 100) / 10,
                            4 => (amount_produced * 100) / 12,
                            5 => (amount_produced * 100) / 14,
                            6 => (amount_produced * 100) / 23,
                            7 => (amount_produced * 100) / 35,
                            _ => 0
                        };
                    }
                    break;
                default:
                    Effiency = 0;
                    break;
            }
            return Effiency;
        }
    }
}
