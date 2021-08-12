﻿using MetaverseMax.ServiceClass;
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
                            historyProductionDetails.run_datetime = common.TimeFormatStandard(historyItem.Value<string>("event_time") ?? "", null);                            

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

                                    historyProductionDetails.efficiency_p = CalculateEfficiency_Production(buildingType, historyLand.Value<int?>("building_level") ?? 0, historyProductionDetails.amount_produced, historyData.Value<int?>("resourceId") ?? 0);
                                    historyProductionDetails.efficiency_m = CalculateEfficiency_MinMax(buildingType, historyLand.Value<int?>("building_level") ?? 0, historyProductionDetails.amount_produced, historyData.Value<int?>("resourceId") ?? 0);

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
                formatedTotal.Add (string.Concat("[", resourceTotal[count].resouceName, "] ", resourceTotal[count].resourceTotal.ToString()));
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

        private static int CalculateEfficiency_Production(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
        {
            double efficiency = 0;

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.ENERGY:
                    if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 750,
                            2 => (amount_produced * 100d) / 836,
                            3 => (amount_produced * 100d) / 938,
                            4 => (amount_produced * 100d) / 1071,
                            5 => (amount_produced * 100d) / 1286,
                            6 => (amount_produced * 100d) / 2143,
                            7 => (amount_produced * 100d) / 3321,
                            _=> 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 5000,
                            2 => (amount_produced * 100d) / 5571,
                            3 => (amount_produced * 100d) / 6250,
                            4 => (amount_produced * 100d) / 7143,
                            5 => (amount_produced * 100d) / 8571,
                            6 => (amount_produced * 100d) / 14286,
                            7 => (amount_produced * 100d) / 22143,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.COMMERCIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.INDUSTRIAL:
                    if (buildingProduct == (int)BUILDING_PRODUCT.METAL)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 160,
                            2 => (amount_produced * 100d) / 178,
                            3 => (amount_produced * 100d) / 200,
                            4 => (amount_produced * 100d) / 229,
                            5 => (amount_produced * 100d) / 274,
                            6 => (amount_produced * 100d) / 457,
                            7 => (amount_produced * 100d) / 594,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.WOOD)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 375,
                            2 => (amount_produced * 100d) / 418,
                            3 => (amount_produced * 100d) / 469,
                            4 => (amount_produced * 100d) / 536,
                            5 => (amount_produced * 100d) / 643,
                            6 => (amount_produced * 100d) / 1071,
                            7 => (amount_produced * 100d) / 1393,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.SAND)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 225,
                            2 => (amount_produced * 100d) / 251,
                            3 => (amount_produced * 100d) / 281,
                            4 => (amount_produced * 100d) / 321,
                            5 => (amount_produced * 100d) / 386,
                            6 => (amount_produced * 100d) / 643,
                            7 => (amount_produced * 100d) / 836,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STONE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 100,
                            2 => (amount_produced * 100d) / 111,
                            3 => (amount_produced * 100d) / 125,
                            4 => (amount_produced * 100d) / 143,
                            5 => (amount_produced * 100d) / 171,
                            6 => (amount_produced * 100d) / 286,
                            7 => (amount_produced * 100d) / 371,
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.OFFICE:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.PRODUCTION:
                    if (buildingProduct == (int)BUILDING_PRODUCT.BRICK)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 40,
                            2 => (amount_produced * 100d) / 45,
                            3 => (amount_produced * 100d) / 50,
                            4 => (amount_produced * 100d) / 57,
                            5 => (amount_produced * 100d) / 69,
                            6 => (amount_produced * 100d) / 114,
                            7 => (amount_produced * 100d) / 177,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.GLASS)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 32,
                            2 => (amount_produced * 100d) / 36,
                            3 => (amount_produced * 100d) / 40,
                            4 => (amount_produced * 100d) / 46,
                            5 => (amount_produced * 100d) / 55,
                            6 => (amount_produced * 100d) / 91,
                            7 => (amount_produced * 100d) / 142,
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced * 100d) / 8,
                            2 => (amount_produced * 100d) / 9,
                            3 => (amount_produced * 100d) / 10,
                            4 => (amount_produced * 100d) / 12,
                            5 => (amount_produced * 100d) / 14,
                            6 => (amount_produced * 100d) / 23,
                            7 => (amount_produced * 100d) / 35,
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }
            return (int)Math.Round(efficiency);
        }

        private static int CalculateEfficiency_MinMax(int buildingType, int buildingLvl, int amount_produced, int buildingProduct)
        {
            double efficiency = 0.0;

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.ENERGY:
                    if (buildingProduct == (int)BUILDING_PRODUCT.WATER)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_1) * 100d / (750 - (int)MIN_WATER_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_2) * 100d / (836 - (int)MIN_WATER_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_3) * 100d / (938 - (int)MIN_WATER_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_4) * 100d / (1071 - (int)MIN_WATER_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_5) * 100d / (1286 - (int)MIN_WATER_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_6) * 100d / (2143 - (int)MIN_WATER_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_WATER_ENUM.LEVEL_7) * 100d / (3321 - (int)MIN_WATER_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.ENERGY)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_1) * 100d /( 5000 - (int)MIN_ENERGY_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_2) * 100d /( 5571 - (int)MIN_ENERGY_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_3) * 100d /( 6250 - (int)MIN_ENERGY_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_4) * 100d /( 7143 - (int)MIN_ENERGY_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_5) * 100d /( 8571 - (int)MIN_ENERGY_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_6) * 100d / (14286 - (int)MIN_ENERGY_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_ENERGY_ENUM.LEVEL_7) * 100d / (22143 - (int)MIN_ENERGY_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.COMMERCIAL:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.INDUSTRIAL:
                    if (buildingProduct == (int)BUILDING_PRODUCT.METAL)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_1) * 100d / (160 - (int)MIN_METAL_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_2) * 100d / (178 - (int)MIN_METAL_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_3) * 100d / (200 - (int)MIN_METAL_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_4) * 100d / (229 - (int)MIN_METAL_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_5) * 100d / (274 - (int)MIN_METAL_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_6) * 100d / (457 - (int)MIN_METAL_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_METAL_ENUM.LEVEL_7) * 100d / (594 - (int)MIN_METAL_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.WOOD)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_1) * 100d / (375 - (int)MIN_WOOD_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_2) * 100d / (418 - (int)MIN_WOOD_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_3) * 100d / (469 - (int)MIN_WOOD_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_4) * 100d / (536 - (int)MIN_WOOD_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_5) * 100d / (643 - (int)MIN_WOOD_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_6) * 100d / (1071 - (int)MIN_WOOD_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_WOOD_ENUM.LEVEL_7) * 100d / (1393 - (int)MIN_WOOD_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.SAND)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_1) * 100d / (225 - (int)MIN_SAND_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_2) * 100d / (251 - (int)MIN_SAND_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_3) * 100d / (281 - (int)MIN_SAND_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_4) * 100d / (321 - (int)MIN_SAND_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_5) * 100d / (386 - (int)MIN_SAND_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_6) * 100d / (643 - (int)MIN_SAND_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_SAND_ENUM.LEVEL_7) * 100d / (836 - (int)MIN_SAND_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STONE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_1) * 100d / (100 - (int)MIN_STONE_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_2) * 100d / (111 - (int)MIN_STONE_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_3) * 100d / (125 - (int)MIN_STONE_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_4) * 100d / (143 - (int)MIN_STONE_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_5) * 100d / (171 - (int)MIN_STONE_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_6) * 100d / (286 - (int)MIN_STONE_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_STONE_ENUM.LEVEL_7) * 100d / (371 - (int)MIN_STONE_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                case (int)BUILDING_TYPE.OFFICE:
                    efficiency = 0;
                    break;
                case (int)BUILDING_TYPE.PRODUCTION:
                    if (buildingProduct == (int)BUILDING_PRODUCT.BRICK)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_1) * 100d / (40 - (int)MIN_BRICK_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_2) * 100d / (45 - (int)MIN_BRICK_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_3) * 100d / (50 - (int)MIN_BRICK_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_4) * 100d / (57 - (int)MIN_BRICK_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_5) * 100d / (69 - (int)MIN_BRICK_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_6) * 100d / (114 - (int)MIN_BRICK_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_BRICK_ENUM.LEVEL_7) * 100d / (177 - (int)MIN_BRICK_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.GLASS)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_1) * 100d / (32 - (int)MIN_GLASS_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_2) * 100d / (36 - (int)MIN_GLASS_ENUM.LEVEL_2),
                            3 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_3) * 100d / (40 - (int)MIN_GLASS_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_4) * 100d / (46 - (int)MIN_GLASS_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_5) * 100d / (55 - (int)MIN_GLASS_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_6) * 100d / (91 - (int)MIN_GLASS_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_GLASS_ENUM.LEVEL_7) * 100d / (142 - (int)MIN_GLASS_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    else if (buildingProduct == (int)BUILDING_PRODUCT.STEEL || buildingProduct == (int)BUILDING_PRODUCT.CONCRETE)
                    {
                        efficiency = buildingLvl switch
                        {
                            1 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_1) * 100d / (8 - (int)MIN_CONCRETE_ENUM.LEVEL_1),
                            2 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_2) * 100d / (9 - (int)MIN_CONCRETE_ENUM.LEVEL_3),
                            3 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_3) * 100d / (10 - (int)MIN_CONCRETE_ENUM.LEVEL_3),
                            4 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_4) * 100d / (12 - (int)MIN_CONCRETE_ENUM.LEVEL_4),
                            5 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_5) * 100d / (14 - (int)MIN_CONCRETE_ENUM.LEVEL_5),
                            6 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_6) * 100d / (23 - (int)MIN_CONCRETE_ENUM.LEVEL_6),
                            7 => (amount_produced - (int)MIN_CONCRETE_ENUM.LEVEL_7) * 100d / (35 - (int)MIN_CONCRETE_ENUM.LEVEL_7),
                            _ => 0
                        };
                    }
                    break;
                default:
                    efficiency = 0;
                    break;
            }
            return (int)Math.Round(efficiency);
        }
    }
}