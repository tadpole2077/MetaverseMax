using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
     public class Building
     {
        public int GetSalePrice(JToken saleData)
        {
            long salePriceLarge = 0;
            if (saleData != null && saleData.HasValues && (saleData.Value<bool?>("active") ?? false))
            {
                salePriceLarge = saleData.Value<long?>("sellPrice") ?? 0;
            }

            return (int)(salePriceLarge / 1000000);
        }

        public IEnumerable<DistrictPlot> DistrictPlots(IEnumerable<OwnerLand> ownerLands)
        {

            int[] districts = ownerLands.GroupBy(row => row.district_id).Select(row => row.FirstOrDefault()).Select(row => row.district_id).ToArray();
            DistrictPlot[] districtPlots = new DistrictPlot[districts.Length];
            int count = 0;

            foreach (int districtId in districts)
            {
                districtPlots[count] = new();
                districtPlots[count].district = new int[3] {
                    districtId,
                    ownerLands.Where(row => row.district_id == districtId).Count(),
                    ownerLands.Where(row => row.district_id == districtId && row.forsale_price > 0).Count()
                };
                count++;
            }

            districtPlots = districtPlots.OrderByDescending(row => row.district[1]).ToArray();
   
            return districtPlots;
        }

        public string BuildingImg(int buildingType, int buildingID, int buildingLvl)
        {
            string buildingImg = string.Empty;

            switch (buildingType)
            {
                case (int)BUILDINGTYPE_ENUM.RESIDENTIAL:
                    if (buildingLvl <= 5)
                    {
                        //buildingImg = "29";
                        buildingImg = string.Concat("https://play.mcp3d.com/assets/images/buildings/Residential2_LVL", Convert.ToString(buildingLvl), "-1.png");
                    }
                    else
                    {
                        buildingImg = string.Concat("https://play.mcp3d.com/assets/images/buildings/Residential2_Mega-1.png");
                    }
                    break;
                case (int)BUILDINGTYPE_ENUM.ENERGY:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.WATER_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/WaterPlant_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/WaterPlant_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/WaterPlant_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/WaterPlant_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.POWER_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Energy_ThermalPower_LVL1-1.png"
                        },
                    };                    
                    //buildingImg = "5";                   
                    break;
                case (int)BUILDINGTYPE_ENUM.COMMERCIAL:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.TRADE_CENTER => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.SUPERMARKET => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Supermarket_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Supermarket_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Supermarket_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Supermarket_V2_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Blockmart_V2_LVL1-1.png"
                        },
                    };

                    //buildingImg = "64";
                    break;
                case (int)BUILDINGTYPE_ENUM.INDUSTRIAL:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.SMELTER_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.MIXING_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/MixingPlant_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/MixingPlant_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/MixingPlant_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/MixingPlant_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/MetalSmelterFactory_V2_LVL1-1.png"
                        },
                    };                  
                    break;
                case (int)BUILDINGTYPE_ENUM.OFFICE:
                    if (buildingLvl <= 5)
                    {
                        buildingImg = string.Concat("https://play.mcp3d.com/assets/images/buildings/OfficeRing_LVL", Convert.ToString(buildingLvl), "-1.png");
                        //buildingImg = "30";
                    }
                    else if (buildingLvl == 6)
                    {
                        buildingImg = "https://play.mcp3d.com/assets/images/buildings/OfficeBlock_Huge-1.png";
                    }
                    else if (buildingLvl == 7)
                    {
                        buildingImg = "https://play.mcp3d.com/assets/images/buildings/OfficeRing_Mega-1.png";
                    }
                    break;

                case (int)BUILDINGTYPE_ENUM.PRODUCTION:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.GLASSWORKS => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/GlassFactory_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/GlassFactory_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/GlassFactory_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/GlassFactory_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.BRICKWORKS => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.CONCRETE_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/BrickFactory_V2_LVL1-1.png"
                        },
                    };               
                    break;

                case (int)BUILDINGTYPE_ENUM.MUNICIPAL:                    
                    buildingImg = "28";
                    break;

                case (int)BUILDINGTYPE_ENUM.POI:                    
                    buildingImg = buildingID switch
                    {
                        100 => "./images/OfficeMonument.PNG",
                        104 => "./images/IndustrialMonument.PNG",
                        110 => "./images/ResidentialMonument.PNG",
                        112 => "./images/EnergyMonument.PNG",
                        _ => "./images/POI.png",
                    };
                    break;
                    
                default:
                    buildingImg = "2";
                    break;
            }
            ///assets/images/buildings/Factory_V2_Mega-1.png
            // Plot example: X125 Y164


            return buildingImg.Length <10 ? string.Concat("https://mcp3d.com/api/image/land/", buildingImg) : buildingImg;
        }


        public string BuildingType(int buildingType, int buildingID)
        {
            // Using Switch expression -- cleaner code
            string buildingTypeDesc = buildingType switch
            {
                1 => "Residential",
                3 => "Energy",
                4 => "Commercial",
                5 => "Industrial",
                6 => "Office",
                7 => "Production",
                8 => "Municipal",
                100 => buildingID switch
                {
                    100 => "Office Monument",
                    101 => "Office Landmark",
                    104 => "Industrial Monument",
                    105 => "Industrial Landmark",
                    110 => "Residential Monument",               
                    111 => "Residential Landmark",
                    112 => "Energy Monument",
                    113 => "Energy Landmark",
                    _ => "Landmark",
                },
                _ => "None",
            };
            return buildingTypeDesc;
        }
    }
    public enum BUILDINGTYPE_ENUM
    {
        RESIDENTIAL = 1,
        ENERGY = 3,
        COMMERCIAL = 4,
        INDUSTRIAL = 5,
        OFFICE = 6,
        PRODUCTION = 7,
        MUNICIPAL = 8,
        POI = 100
    }

    public enum BUILDING_PRODUCT
    {
        WOOD = 1,
        SAND = 2,        
        STONE = 3,
        METAL = 4,
        BRICK = 5,
        GLASS = 6,
        WATER = 7,        
        ENERGY = 8,
        STEEL = 9,
        CONCRETE =10
    }

    public enum BUILDING_SUBTYPE  //MCP.building_id
    {   
        COMDOMINIUM = 1,
        APARTMENTS = 2,
        POLICE =4,
        FIRE_STATION = 5,
        HOSPITAL = 6,
        SUPERMARKET =7,
        TRADE_CENTER =8,
        CONCRETE_PLANT =9,
        FACTORY = 10,
        WATER_PLANT = 11,
        POWER_PLANT = 12,
        SMELTER_PLANT = 13,
        MIXING_PLANT = 14,
        BUSINESS_CENTER = 15,
        OFFICE_BLOCK = 16,
        GLASSWORKS =17,
        BRICKWORKS = 18,
    }

}
