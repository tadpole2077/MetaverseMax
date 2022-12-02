namespace MetaverseMax.ServiceClass
{    
    public enum APPLICATION
    {                
        WHITE_AIR_CON = 1,
        CCTV_WHITE = 2,
        RED_FIRE_ALARM = 3,
        ROUTER_BLACK = 4,
        WHITE_SAT = 5,
        RED_SAT = 7,
        GREEN_AIR_CON = 8,
        CCTV_RED = 10           
    }

    public enum CITIZEN_HISTORY {
        DAYS = -40,
        CORRECTION_SECONDS = 45
    }

    public enum HISTORY_TYPE
    {
        CITIZEN = 4,
        PET = 5
    }

    public enum RETURN_CODE
    {
        ERROR = -1,
        SUCCESS = 1
    }

    public enum ACTION_TYPE
    {
        OFFER = 1,
        PET = 2,
        CITIZEN = 3,
        PLOT = 4,
        ASSIGN_CITIZEN = 10,
        REMOVE_CITIZEN = 11,
        ASSIGN_PET = 12,
        REMOVE_PET = 13,
        NEW_OWNER = 14,
        PET_NEW_OWNER = 15
    }

    public enum PET_LOOK
    {
        BULLDOG = 1,
        CORGI_DOG = 2,
        LABRADOR = 3,
        MASTIFF_DOG = 4,
        WHIPPET_DOG = 5,
        PARROT = 6,
        LION = 12,
        RED_DRAGON = 15,
        CHAMELEON = 16,
        BEETLE = 254
    }

    public enum PET_BONUS_TYPE
    {
        CHARISMA = 1,
        LUCK = 2,
        AGILITY = 3,
        STRENGTH = 4,
        ENDURANCE= 5,
        INTEL = 6
    }

    public enum TRAIT_INDEX
    {
        STRENGTH = 1,
        ENDURANCE = 2,
        CHARISMA = 3,
        INTELLIGENCE = 4,
        AGILITY = 5,
        LUCK = 6
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
        CONCRETE = 10,
        FACTORY_PRODUCT = 99
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
        STEEL_PLANT = 9,
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

    public enum ELECTRIC_RESOURCE
    {
        LEVEL_1 = 0,
        LEVEL_2 = 3,
        LEVEL_3 = 10,
        LEVEL_4 = 17,
        LEVEL_5 = 24,
        LEVEL_6 = 31,
        LEVEL_7 = 45,
        LEVEL_8 = 52,
        LEVEL_9 = 66,
        LEVEL_10 = 100
    }

    public enum WATER_RESOURCE
    {
        LEVEL_1 = 0,
        LEVEL_2 = 3,
        LEVEL_3 = 10,
        LEVEL_4 = 17,
        LEVEL_5 = 24,
        LEVEL_6 = 31,
        LEVEL_7 = 45,
        LEVEL_8 = 52,
        LEVEL_9 = 66,
        LEVEL_10 = 100
    }

    public enum DAMAGE_EFF
    {
        DMG_1_TO_5 = 40,
        DMG_1_TO_20 = 55,
        DMG_1_TO_35 = 69,
        DMG_1_TO_50 = 80,
        DMG_1_TO_65 = 92,
        DMG_1_TO_80 = 104,
        DMG_1_TO_90 = 111
    }

    public enum MIN_WOOD
    {
        LEVEL_1 = 150,
        LEVEL_2 = 167,
        LEVEL_3 = 188,
        LEVEL_4 = 214,
        LEVEL_5 = 257,
        LEVEL_6 = 429,
        LEVEL_7 = 557
    }
    public enum MAX_WOOD
    {
        LEVEL_1 = 375,
        LEVEL_2 = 418,
        LEVEL_3 = 469,
        LEVEL_4 = 536,
        LEVEL_5 = 643,
        LEVEL_6 = 1071,
        LEVEL_7 = 1393
    }
    public enum MIN_METAL
    {
        LEVEL_1 = 40,
        LEVEL_2 = 45,
        LEVEL_3 = 50,
        LEVEL_4 = 57,
        LEVEL_5 = 69,
        LEVEL_6 = 114,
        LEVEL_7 = 149
    }
    public enum MAX_METAL
    {
        LEVEL_1 = 160,
        LEVEL_2 = 178,
        LEVEL_3 = 200,
        LEVEL_4 = 229,
        LEVEL_5 = 274,
        LEVEL_6 = 457,
        LEVEL_7 = 594
    }
    public enum MIN_SAND
    {
        LEVEL_1 = 75,
        LEVEL_2 = 84,
        LEVEL_3 = 94,
        LEVEL_4 = 107,
        LEVEL_5 = 129,
        LEVEL_6 = 214,
        LEVEL_7 = 279
    }
    public enum MAX_SAND
    {
        LEVEL_1 = 225,
        LEVEL_2 = 251,
        LEVEL_3 = 281,
        LEVEL_4 = 321,
        LEVEL_5 = 386,
        LEVEL_6 = 643,
        LEVEL_7 = 836
    }
    public enum MAX_STONE
    {
        LEVEL_1 = 100,
        LEVEL_2 = 111,
        LEVEL_3 = 125,
        LEVEL_4 = 143,
        LEVEL_5 = 171,
        LEVEL_6 = 286,
        LEVEL_7 = 371
    }
    public enum MIN_STONE
    {
        LEVEL_1 = 20,
        LEVEL_2 = 22,
        LEVEL_3 = 25,
        LEVEL_4 = 29,
        LEVEL_5 = 34,
        LEVEL_6 = 57,
        LEVEL_7 = 74
    }
    public enum MIN_ENERGY
    {
        LEVEL_1 = 1000,
        LEVEL_2 = 1114,
        LEVEL_3 = 1250,
        LEVEL_4 = 1429,
        LEVEL_5 = 1714,
        LEVEL_6 = 2857,
        LEVEL_7 = 4429
    }
    public enum MAX_ENERGY
    {
        LEVEL_1 = 5000,
        LEVEL_2 = 5571,
        LEVEL_3 = 6250,
        LEVEL_4 = 7143,
        LEVEL_5 = 8571,
        LEVEL_6 = 14286,
        LEVEL_7 = 22143
    }
    public enum MIN_WATER
    {
        LEVEL_1 = 250,
        LEVEL_2 = 279,
        LEVEL_3 = 313,
        LEVEL_4 = 357,
        LEVEL_5 = 429,
        LEVEL_6 = 714,
        LEVEL_7 = 1107
    }
    public enum MAX_WATER
    {
        LEVEL_1 = 750,
        LEVEL_2 = 836,
        LEVEL_3 = 938,
        LEVEL_4 = 1071,
        LEVEL_5 = 1286,
        LEVEL_6 = 2143,
        LEVEL_7 = 3321
    }
    public enum MIN_BRICK
    {
        LEVEL_1 = 8,
        LEVEL_2 = 9,
        LEVEL_3 = 10,
        LEVEL_4 = 11,
        LEVEL_5 = 14,
        LEVEL_6 = 23,
        LEVEL_7 = 35
    }
    public enum MAX_BRICK
    {
        LEVEL_1 = 40,
        LEVEL_2 = 45,
        LEVEL_3 = 50,
        LEVEL_4 = 57,
        LEVEL_5 = 69,
        LEVEL_6 = 114,
        LEVEL_7 = 177
    }
    public enum MIN_GLASS
    {
        LEVEL_1 = 4,
        LEVEL_2 = 4,
        LEVEL_3 = 5,
        LEVEL_4 = 6,
        LEVEL_5 = 7,
        LEVEL_6 = 11,
        LEVEL_7 = 18
    }
    public enum MAX_GLASS
    {
        LEVEL_1 = 32,
        LEVEL_2 = 36,
        LEVEL_3 = 40,
        LEVEL_4 = 46,
        LEVEL_5 = 55,
        LEVEL_6 = 91,
        LEVEL_7 = 142
    }
    public enum MIN_STEEL
    {
        LEVEL_1 = 1,
        LEVEL_2 = 2,
        LEVEL_3 = 3,
        LEVEL_4 = 3,
        LEVEL_5 = 3,
        LEVEL_6 = 6,
        LEVEL_7 = 9
    }
    public enum MAX_STEEL
    {
        LEVEL_1 = 8,
        LEVEL_2 = 9,
        LEVEL_3 = 10,
        LEVEL_4 = 12,
        LEVEL_5 = 14,
        LEVEL_6 = 23,
        LEVEL_7 = 35
    }
    public enum MIN_CONCRETE
    {
        LEVEL_1 = 1,
        LEVEL_2 = 2,
        LEVEL_3 = 3,
        LEVEL_4 = 3,
        LEVEL_5 = 3,
        LEVEL_6 = 6,
        LEVEL_7 = 9
    }
    public enum MAX_CONCRETE
    {
        LEVEL_1 = 8,
        LEVEL_2 = 9,
        LEVEL_3 = 10,
        LEVEL_4 = 12,
        LEVEL_5 = 14,
        LEVEL_6 = 23,
        LEVEL_7 = 35
    }

    public enum WORLD_TYPE
    {
        TRON = 1,
        ETH = 2
    }
}