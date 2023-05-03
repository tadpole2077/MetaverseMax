using Newtonsoft.Json.Linq;

namespace MetaverseMax.ServiceClass
{
    public class Building
    {
        //
        // format differs per World token type
        //    TRON :  6 places - need to divide by 1 million to get value in Tron.
        //    BNB  :  18 places - need to divide by 1,000,000,000,000,000,000 to get integer value in BNB
        //    ETH  :  18 places 
        public decimal GetSalePrice(JToken saleData, WORLD_TYPE worldTypeSelected)
        {
            return GetPrice(saleData, worldTypeSelected, "sellPrice", true);
        }

        public decimal GetPrice(JToken saleData, WORLD_TYPE worldTypeSelected, string fieldName, bool checkActive)
        {
            decimal salePriceLarge = 0;
            if (saleData != null && saleData.HasValues && (checkActive || (saleData.Value<bool?>("active") ?? false)))
            {
                salePriceLarge = saleData.Value<decimal?>(fieldName) ?? 0;
            }

            salePriceLarge = worldTypeSelected switch
            {
                WORLD_TYPE.TRON => salePriceLarge / 1000000,                   // 6 places back
                WORLD_TYPE.BNB => salePriceLarge / 1000000000000000000,        // 18 places back
                WORLD_TYPE.ETH => salePriceLarge / 1000000000000000000,        // 18 places back
                _ => salePriceLarge / 1000000
            };

            return salePriceLarge;

        }


        public decimal GetRentPrice(JToken rentData, WORLD_TYPE worldTypeSelected)
        {
            decimal priceLarge = 0;
            if (rentData != null && rentData.HasValues)
            {
                priceLarge = rentData.Value<decimal?>("price") ?? 0;
            }

            priceLarge = worldTypeSelected switch
            {
                WORLD_TYPE.TRON => priceLarge / 1000000,                   // 6 places back
                WORLD_TYPE.BNB => priceLarge / 1000000000000000000,        // 18 places back
                WORLD_TYPE.ETH => priceLarge / 1000000000000000000,        // 18 places back
                _ => priceLarge / 1000000
            };

            return priceLarge;
        }

        // Create an array of districts with summary data matching plots owned by player
        public IEnumerable<DistrictPlot> DistrictPlots(IEnumerable<OwnerLand> ownerLands)
        {

            int[] districts = ownerLands.GroupBy(row => row.district_id)
                                        .Select(row => row.FirstOrDefault())
                                        .OrderBy(row => row.district_id)
                                        .Select(row => row.district_id)
                                        .ToArray();

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

            //districtPlots = districtPlots.OrderByDescending(row => row.district[1]).ToArray();

            return districtPlots;
        }

        public string GetBuildingImg(int buildingType, int buildingID, int buildingLvl, WORLD_TYPE worldType)
        {
            string buildingImg = string.Empty;

            switch (buildingType)
            {
                case (int)BUILDING_TYPE.RESIDENTIAL:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.CONDOMINIUM => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Residential3_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Residential3_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Residential3_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Residential3_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.APARTMENTS => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Residential1_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Residential1_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Residential1_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Residential1_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.VILLA => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Residential2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Residential2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Residential2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Residential2_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Residential3_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Residential3_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Residential3_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Residential3_LVL1-1.png"
                        },
                    };
                    break;

                case (int)BUILDING_TYPE.ENERGY:
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
                    break;

                case (int)BUILDING_TYPE.COMMERCIAL:
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
                    break;

                case (int)BUILDING_TYPE.INDUSTRIAL:
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

                case (int)BUILDING_TYPE.OFFICE:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.OFFICE_BLOCK => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.BUSINESS_CENTER => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/OfficeBlock_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/OfficeBlock_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/OfficeBlock_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/OfficeBlock_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/OfficeRing_LVL1-1.png"
                        },
                    };
                    break;

                case (int)BUILDING_TYPE.PRODUCTION:
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
                        (int)BUILDING_SUBTYPE.CONCRETE_PLANT => worldType switch
                        {
                            WORLD_TYPE.TRON => buildingLvl switch
                            {
                                <= 5 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                                6 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Huge-1.png",
                                7 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Mega-1.png",
                                _ => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL1-1.png"
                            },
                            WORLD_TYPE.ETH => buildingLvl switch
                            {
                                <= 5 => "https://play.mcp3d.com/assets/images/buildings/SteelMill_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                                6 => "https://play.mcp3d.com/assets/images/buildings/SteelMill_V2_Huge-1.png",
                                7 => "https://play.mcp3d.com/assets/images/buildings/SteelMill_V2_Mega-1.png",
                                _ => "https://play.mcp3d.com/assets/images/buildings/SteelMill_V2_LVL1-1.png"
                            },
                            WORLD_TYPE.BNB => buildingLvl switch
                            {
                                <= 5 => "https://play.mcp3d.com/assets/images/buildings/Plastic_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                                6 => "https://play.mcp3d.com/assets/images/buildings/Plastic_Huge-1.png",
                                7 => "https://play.mcp3d.com/assets/images/buildings/Plastic_Mega-1.png",
                                _ => "https://play.mcp3d.com/assets/images/buildings/Plastic_LVL1-1.png"
                            },
                            _ => buildingLvl switch
                            {
                                <= 5 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                                6 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Huge-1.png",
                                7 => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_Mega-1.png",
                                _ => "https://play.mcp3d.com/assets/images/buildings/ConcreteMill_LVL1-1.png"
                            },
                        },
                        (int)BUILDING_SUBTYPE.FACTORY => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Factory_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Factory_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Factory_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Factory_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.CHEMICAL_PLANT => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/ChemicalPlant_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/ChemicalPlant_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/ChemicalPlant_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/ChemicalPlant_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.PAPER_FACTORY => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/PaperFactory_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/PaperFactory_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/PaperFactory_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/PaperFactory_LVL1-1.png"
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

                case (int)BUILDING_TYPE.MUNICIPAL:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.HOSPITAL => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.POLICE => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/PoliceDept_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/PoliceDept_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/PoliceDept_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/PoliceDept_V2_LVL1-1.png"
                        },
                        (int)BUILDING_SUBTYPE.FIRE_STATION => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/FireDept_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/FireDept_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/FireDept_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/FireDept_V2_LVL1-1.png"
                        },
                        _ => buildingLvl switch
                        {
                            <= 5 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_LVL" + Convert.ToString(buildingLvl) + "-1.png",
                            6 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_Huge-1.png",
                            7 => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_Mega-1.png",
                            _ => "https://play.mcp3d.com/assets/images/buildings/Hospital_V2_LVL1-1.png"
                        },
                    };
                    break;

                case (int)BUILDING_TYPE.POI:
                    buildingImg = buildingID switch
                    {
                        (int)BUILDING_SUBTYPE.OFFICE_MONUMENT => "./images/OfficeMonument.PNG",
                        (int)BUILDING_SUBTYPE.INDUSTRY_MONUMENT => "./images/IndustrialMonument.PNG",
                        (int)BUILDING_SUBTYPE.PRODUCTION_MONUMENT => "./images/ProductionMonument.PNG",
                        (int)BUILDING_SUBTYPE.COMMERCIAL_MONUMENT => "./images/CommercialMonument.PNG",
                        (int)BUILDING_SUBTYPE.RESIDENTIAL_MONUMENT => "./images/ResidentialMonument.PNG",
                        (int)BUILDING_SUBTYPE.ENERGY_MONUMENT => "./images/EnergyMonument.PNG",
                        _ => "./images/POI.png",
                    };
                    break;

                default:
                    buildingImg = "2";
                    break;
            }
            ///assets/images/buildings/Factory_V2_Mega-1.png
            // Plot example: X125 Y164


            return buildingImg.Length < 10 ? string.Concat("https://mcp3d.com/api/image/land/", buildingImg) : buildingImg;
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
                    106 => "Production Monument",
                    107 => "Production Landmark",
                    108 => "Commercial Monument",
                    109 => "Commercial Landmark",
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

}
