namespace MetaverseMax.ServiceClass
{
    public enum PET_BONUS_TYPE
    {
        CHARISMA = 1,
        LUCK = 2,
        AGILITY = 3,
        STRENGTH = 4,
        ENDURANCE= 5,
        INTEL = 6
    }

    public enum TOKEN_TYPE
    {
        PLOT = 1,
        DISTRICT = 2,
        CITIZEN = 4,
        PET = 5,
        APPLICATION = 6,
        RESOURCE = 7,
        CAR = 8
    }
    public enum DISTRICT_PERKS
    {
        TWINS_RESIDENTIAL = 1,
        FASTER_PRODUCTION_INDUSTRY = 2,
        FASTER_PRODUCTION_PRODUCTION = 3,
        FASTER_PRODUCTION_ENERGY = 4,
        DOUBLE_COLLECT_INDUSTRIAL = 5,
        DOUBLE_COLLECT_PRODUCTION = 6,
        DOUBLE_COLLECT_ENERGY = 7,
        INCREASE_POI_RANGE_OFFICE = 8,
        INCREASE_POI_RANGE_MUNICIPAL = 9,
        INCREASE_POI_RANGE_INDUSTRIAL = 10,
        INCREASE_POI_RANGE_PRODUCTION = 11,
        INCREASE_POI_RANGE_COMMERCIAL = 12,
        INCREASE_POI_RANGE_RESIDENTIAL = 13,
        INCREASE_POI_RANGE_ENERGY = 14,
        EXTRA_SLOT_APPLIANCE_ALL_BUILDINGS = 15,
        EXTRA_STAMINA_APPLIANCE_ALL_BUILDINGS = 16
    }

    public enum BUILDING_TYPE
    {
        EMPTY_LAND = 0,
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
        CONCRETE = 10
    }

    public enum BUILDING_SUBTYPE  //MCP.building_id
    {
        COMDOMINIUM = 1,
        APARTMENTS = 2,
        VILLA = 3,
        POLICE = 4,
        FIRE_STATION = 5,
        HOSPITAL = 6,
        SUPERMARKET = 7,
        TRADE_CENTER = 8,
        CONCRETE_PLANT = 9,
        FACTORY = 10,
        WATER_PLANT = 11,
        POWER_PLANT = 12,
        SMELTER_PLANT = 13,
        MIXING_PLANT = 14,
        BUSINESS_CENTER = 15,
        OFFICE_BLOCK = 16,
        GLASSWORKS = 17,
        BRICKWORKS = 18,
    }

    public enum MIN_WOOD_ENUM
    {
        LEVEL_1 = 150,
        LEVEL_2 = 167,
        LEVEL_3 = 188,
        LEVEL_4 = 214,
        LEVEL_5 = 257,
        LEVEL_6 = 429,
        LEVEL_7 = 557
    }
    public enum MIN_METAL_ENUM
    {
        LEVEL_1 = 40,
        LEVEL_2 = 45,
        LEVEL_3 = 50,
        LEVEL_4 = 57,
        LEVEL_5 = 69,
        LEVEL_6 = 114,
        LEVEL_7 = 149
    }

    public enum MIN_SAND_ENUM
    {
        LEVEL_1 = 75,
        LEVEL_2 = 84,
        LEVEL_3 = 94,
        LEVEL_4 = 107,
        LEVEL_5 = 129,
        LEVEL_6 = 214,
        LEVEL_7 = 279
    }
    public enum MIN_STONE_ENUM
    {
        LEVEL_1 = 20,
        LEVEL_2 = 22,
        LEVEL_3 = 25,
        LEVEL_4 = 29,
        LEVEL_5 = 34,
        LEVEL_6 = 57,
        LEVEL_7 = 74
    }
    public enum MIN_ENERGY_ENUM
    {
        LEVEL_1 = 1000,
        LEVEL_2 = 1114,
        LEVEL_3 = 1250,
        LEVEL_4 = 1429,
        LEVEL_5 = 1714,
        LEVEL_6 = 2857,
        LEVEL_7 = 4429
    }
    public enum MIN_WATER_ENUM
    {
        LEVEL_1 = 250,
        LEVEL_2 = 279,
        LEVEL_3 = 313,
        LEVEL_4 = 357,
        LEVEL_5 = 429,
        LEVEL_6 = 714,
        LEVEL_7 = 1107
    }
    public enum MIN_BRICK_ENUM
    {
        LEVEL_1 = 8,
        LEVEL_2 = 9,
        LEVEL_3 = 10,
        LEVEL_4 = 11,
        LEVEL_5 = 14,
        LEVEL_6 = 23,
        LEVEL_7 = 35
    }

    public enum MIN_GLASS_ENUM
    {
        LEVEL_1 = 4,
        LEVEL_2 = 4,
        LEVEL_3 = 5,
        LEVEL_4 = 6,
        LEVEL_5 = 7,
        LEVEL_6 = 11,
        LEVEL_7 = 18
    }
    public enum MIN_CONCRETE_ENUM
    {
        LEVEL_1 = 1,
        LEVEL_2 = 2,
        LEVEL_3 = 3,
        LEVEL_4 = 3,
        LEVEL_5 = 3,
        LEVEL_6 = 6,
        LEVEL_7 = 9
    }

    public enum WORLD_TYPE
    {
        TRON = 1,
        ETH = 2
    }
}