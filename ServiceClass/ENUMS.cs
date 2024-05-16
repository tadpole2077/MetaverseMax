namespace MetaverseMax.BaseClass
{        
    public enum ACTIVE_BUILDING
    {
        DAYS = 9
    } 

    public enum APPLICATION_ID
    {
        WHITE_AIR_CON = 1,
        CCTV_WHITE = 2,
        RED_FIRE_ALARM = 3,
        ROUTER_BLACK = 4,
        WHITE_SAT = 5,
        GREEN_SAT = 6,
        RED_SAT = 7,
        GREEN_AIR_CON = 8,
        RED_AIR_CON = 9,
        CCTV_RED = 10
    }
    public enum APPLICATION_BONUS
    {
        RED_FIRE_ALARM = 1,
        ROUTER_BLACK = 2,
        CCTV_WHITE = 3,
        CCTV_RED = 4,
        WHITE_AIR_CON = 5,
        GREEN_AIR_CON = 6,
        RED_AIR_CON = 7,
        WHITE_SAT = 8,
        GREEN_SAT = 9,
        RED_SAT = 10
    }

    public enum CITIZEN_HISTORY
    {
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
        SUCCESS = 1,
        SECURITY_FAIL = -2
    }

    public enum DISTRIBUTE_ACTION
    {
        GET_DISTRICT_FUND = 1,
        CALC_DISTRICT_DISTRIBUTION = 2,
        GET_GLOBAL_FUND = 3,
        CALC_GLOBAL_DISTRIBUTION = 4,
        REFRESH_GLOBAL_FUND = 5,
        REFRESH_DISTRICT_FUND = 6,
        REFRESH = 7
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

    public enum ALERT_ACTION_TYPE
    {
        ENABLE = 1,
        DISABLE = 2
    }
    public enum ALERT_TYPE
    {
        NOT_USED = 0,
        INITIAL_LAND_VALUE = 1,
        CONSTRUCTION_TAX = 2,
        PRODUCTION_TAX = 3,
        DISTRIBUTION = 4,
        BUILDING_RANKING = 5,
        NEW_BUILDING = 6,
    }

    public enum ALERT_ICON_TYPE
    {
        INFO = 1,
        TAX = 2,
        STAMINA = 3,
        NEW_OFFER = 4,
        RANKING = 5,
        NEW_BUILDING =6,
    }

    public enum ALERT_ICON_TYPE_CHANGE
    { 
        NONE = 0,
        INCREASE = 1,
        DECREASE = 2
    }

    public enum ALERT_TRIGGER_TYPE
    {
        AUTOMATIC_NO_TRIGGER = 0,
    }

    public enum MIN_STAMINA
    {
        ENERGY = 15,
        RESIDENTIAL = 100,
        OFFICE = 25,
        COMMERCIAL = 5,
        MUNICIPAL = 10,
        INDUSTRIAL = 25,
        PRODUCTION = 50
    }

    public class ALERT_MESSAGE {
        public static readonly string INTRO = "Your alerts are now activated. New alerts will be shown here when identified, and stored in your alert history. Click delete to clear alert from history.";
        public static readonly string LOW_STAMINA = "#CIT_AMOUNT# x Building have low stamina citizens and will stop building activity after next collection cycle.";
        public static readonly string NEW_OFFER = "New offer received from #BIDDER# for your #ASSET#(#ASSET_ID#), offer price #PRICE#.";
        public static readonly string OFFER_ACCEPTED_BY = "Your offer was accepted by #OWNER# for your #ASSET#(#ASSET_ID#), offer price #PRICE#.";
        public static readonly string RANKING_CHANGE = "IP Ranking change : #BUILDING_TYPE# Building level-#LEVEL# (ID:#TOKEN_ID#) in District ##DISTRICT_ID#.\nOld Ranking: #OLD_RANKING#% vs New Ranking: #NEW_RANKING#%.#OWNER#";
        public static readonly string NEW_BUILDING = "New custom building(#BUILDING_NAME#) in District ##DISTRICT_ID# by #OWNER#.";
    }

    public class JOB_SETTING_CODE
    {
        public static readonly string DISABLE_DISTRIBUTION_UPDATE = "DISABLE_DISTRIBUTION_UPDATE";
        public static readonly string NEW_ACCOUNT_PRO_TOOLS_FREE_DAYS = "NEW_ACCOUNT_PRO_TOOLS_FREE_DAYS";
    }
    public class SETTING_CODE
    {
        public static readonly string SHUTDOWN_PENDING = "SHUTDOWN_PENDING";
    }

    public class BANK_ACTION
    {
        public static readonly char DEPOSIT = 'D';
        public static readonly char WITHDRAW = 'W';        
        public static readonly char WITHDRAW_PENDING = 'P';
    }

    public enum ALERT_STATE
    {
        ALL = 1,
        UNREAD = 2,
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
        ENDURANCE = 5,
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
        CAR = 8,
        PARCEL = 10,
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
        PARCEL = 10,
        POI = 100
    }

    public enum LAND_TYPE
    {
        TRON_BUILDABLE_LAND = 1,
        BNB_BUILDABLE_LAND = 3
    }

    public enum BUILDING_SIZE
    {
        HUGE = 6,
        MEGA = 7

    }

    public enum BUILDING_PRODUCT
    {
        UNKNOWN = 0,
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
        PLASTIC = 11,
        GLUE = 12,
        MIXES = 13,
        COMPOSITE = 14,
        PAPER = 15,
        FACTORY_PRODUCT = 99,
        MEGA_PRODUCT_GLOBAL = 200,
        MEGA_PRODUCT_LOCAL = 201,
        COMMERCIAL_SERVICE = 202,
        INSURANCE_COVER_POLICE = 210,
        INSURANCE_COVER_HOSPITAL = 211,
        INSURANCE_COVER_FIRESTATION = 212,
        CITIZEN_PRODUCTION_VILLA = 220,
        CITIZEN_PRODUCTION_APT = 221,
        CITIZEN_PRODUCTION_CONDO = 222,
    }

    public enum BUILDING_SUBTYPE  //MCP.building_id
    {
        CONDOMINIUM = 1,
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
        CHEMICAL_PLANT = 19,
        PAPER_FACTORY = 20,
        OFFICE_MONUMENT = 100,
        OFFICE_LANDMARK = 101,
        INDUSTRY_MONUMENT = 104,
        INDUSTRY_LANDMARK = 105,
        PRODUCTION_MONUMENT = 106,
        PRODUCTION_LANDMARK = 107,
        COMMERCIAL_MONUMENT = 108,
        RESIDENTIAL_MONUMENT = 110,
        RESIDENTIAL_LANDMARK = 111,
        ENERGY_MONUMENT = 112,
        ENERGY_LANDMARK = 113,
        SUBWAY_STATION = 200
    }

    public enum CUSTOM_BUILDING_CATEGORY  
    {
        PARCEL = 0,
        DOWNTOWN = 1,
        HOUSING = 2,
        SHOPPING =3,
        RETREAT = 4,
        ECO = 5,
        LUXURY = 6,
        MIDTOWN = 7,
        HEADQUARTERS =8,
        COUNTRYSIDE =9,
        RURAL = 10,
        MAIN_TOWER = 100
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
    public enum MIN_PLASTIC
    {
        LEVEL_1 = 1,
        LEVEL_2 = 2,
        LEVEL_3 = 3,
        LEVEL_4 = 3,
        LEVEL_5 = 3,
        LEVEL_6 = 6,
        LEVEL_7 = 9
    }
    public enum MAX_PLASTIC
    {
        LEVEL_1 = 8,
        LEVEL_2 = 9,
        LEVEL_3 = 10,
        LEVEL_4 = 12,
        LEVEL_5 = 14,
        LEVEL_6 = 23,
        LEVEL_7 = 35
    }
    public enum MIN_CHEMICAL
    {
        LEVEL_1 = 150,
        LEVEL_2 = 168,
        LEVEL_3 = 186,
        LEVEL_4 = 204,
        LEVEL_5 = 258,
        LEVEL_6 = 435,
        LEVEL_7 = 651
    }
    public enum MAX_CHEMICAL
    {
        LEVEL_1 = 900,
        LEVEL_2 = 1006,
        LEVEL_3 = 1118,
        LEVEL_4 = 1283,
        LEVEL_5 = 1548,
        LEVEL_6 = 2610,
        LEVEL_7 = 3906
    }
    public enum MIN_PAPER
    {
        LEVEL_1 = 240,
        LEVEL_2 = 268,
        LEVEL_3 = 298,
        LEVEL_4 = 342,
        LEVEL_5 = 412,
        LEVEL_6 = 696,
        LEVEL_7 = 1042
    }
    public enum MAX_PAPER
    {
        LEVEL_1 = 960,
        LEVEL_2 = 1074,
        LEVEL_3 = 1192,
        LEVEL_4 = 1368,
        LEVEL_5 = 1652,
        LEVEL_6 = 2784,
        LEVEL_7 = 4166
    }

    public enum WORLD_TYPE
    {
        UNKNOWN = 0,
        TRON = 1,
        ETH = 2,
        BNB = 3,
        UNIVERSE = 9
    }

    public class PRODUCE_IMG
    {
        public static readonly string PAPER = "/assets/images/resources/paper.png";
        public static readonly string CONCRETE = "/assets/images/resources/concrete.png";
        public static readonly string STEEL = "/assets/images/resources/steel.png";
        public static readonly string PLASTIC = "/assets/images/resources/plastic.png";
        public static readonly string MIXES = "/assets/images/resources/mixes.png";
        public static readonly string GLUE = "/assets/images/resources/glue.png";
        public static readonly string COMPOSITE = "/assets/images/resources/composite.png";
        public static readonly string BRICK = "assets/images/resources/brick.png";
    }

    public class MATIC_WS
    {
        public static readonly string ACCOUNT_MATERIAL_GET = "https://misty-dawn-mountain.matic.quiknode.pro/4a6d10967c8875ef8d3488a9efc37234045b809b/";
    }

    public class TRON_WS
    {
        public static readonly string LAND_GET = "https://ws-tron.mcp3d.com/land/get";
        public static readonly string PARCEL_GET = "https://ws-tron.mcp3d.com/parcel/get";
        public static readonly string BUILDING_UNIT_GET = "https://ws-tron.mcp3d.com/units/parcel/";
        public static readonly string OWNER_LANDS = "https://ws-tron.mcp3d.com/user/assets/lands";
        public static readonly string POI_GET = "https://ws-tron.mcp3d.com/poi/get";
        public static readonly string USER_GET = "https://ws-tron.mcp3d.com/user/get";
        public static readonly string USER_PACKS_GET = "https://ws-tron.mcp3d.com/user/assets/packs";
        public static readonly string SALES_OFFER = "https://ws-tron.mcp3d.com/sales/offers";
        public static readonly string SALES_INFO = "https://ws-tron.mcp3d.com/sales/info";
        public static readonly string ASSETS_PETS = "https://ws-tron.mcp3d.com/user/assets/pets";
        public static readonly string ASSETS_HISTORY = "https://ws-tron.mcp3d.com/user/assets/history";
        public static readonly string ASSETS_CITIZENS = "https://ws-tron.mcp3d.com/user/assets/citizens";
        public static readonly string CITIZEN_GET = "https://ws-tron.mcp3d.com/citizen/get";
        public static readonly string REGIONS_LIST = "https://ws-tron.mcp3d.com/regions/list";
        public static readonly string PERKS_DISTRICT = "https://ws-tron.mcp3d.com/perks/districts";
        public static readonly string DISTRICT_INFO = "https://ws-tron.mcp3d.com/newspaper/district/info";
        public static readonly string BALANCES = "https://ws-tron.mcp3d.com/balances";
        public static readonly string MISSION = "https://ws-tron.mcp3d.com/missions/land/";
        public static readonly string CONTRACT = "https://ws.mcp3d.com/contract";

    }

    public class BNB_WS
    {
        public static readonly string LAND_GET = "https://ws-bsc.mcp3d.com/land/get";
        public static readonly string PARCEL_GET = "https://ws-bsc.mcp3d.com/parcel/get";
        public static readonly string BUILDING_UNIT_GET = "https://ws-bsc.mcp3d.com/units/parcel/";
        public static readonly string OWNER_LANDS = "https://ws-bsc.mcp3d.com/user/assets/lands";
        public static readonly string POI_GET = "https://ws-bsc.mcp3d.com/poi/get";
        public static readonly string USER_GET = "https://ws-bsc.mcp3d.com/user/get";
        public static readonly string USER_PACKS_GET = "https://ws-bsc.mcp3d.com/user/assets/packs";
        public static readonly string SALES_OFFER = "https://ws-bsc.mcp3d.com/sales/offers";
        public static readonly string SALES_INFO = "https://ws-bsc.mcp3d.com/sales/info";
        public static readonly string ASSETS_PETS = "https://ws-bsc.mcp3d.com/user/assets/pets";
        public static readonly string ASSETS_HISTORY = "https://ws-bsc.mcp3d.com/user/assets/history";
        public static readonly string ASSETS_CITIZENS = "https://ws-bsc.mcp3d.com/user/assets/citizens";
        public static readonly string CITIZEN_GET = "https://ws-bsc.mcp3d.com/citizen/get";
        public static readonly string REGIONS_LIST = "https://ws-bsc.mcp3d.com/regions/list";
        public static readonly string PERKS_DISTRICT = "https://ws-bsc.mcp3d.com/perks/districts";
        public static readonly string DISTRICT_INFO = "https://ws-bsc.mcp3d.com/newspaper/district/info";
        public static readonly string BALANCES = "https://ws-bsc.mcp3d.com/balances";
        public static readonly string MISSION = "https://ws-bsc.mcp3d.com/missions/land/";
        public static readonly string CONTRACT = "https://ws.mcp3d.com/contract";
    }

    public class ETH_WS
    {
        public static readonly string LAND_GET = "https://ws.mcp3d.com/land/get";
        public static readonly string PARCEL_GET = "https://ws.mcp3d.com/parcel/get";
        public static readonly string BUILDING_UNIT_GET = "https://ws.mcp3d.com/units/parcel/";
        public static readonly string OWNER_LANDS = "https://ws.mcp3d.com/user/assets/lands";
        public static readonly string POI_GET = "https://ws.mcp3d.com/poi/get";
        public static readonly string USER_GET = "https://ws.mcp3d.com/user/get";
        public static readonly string USER_PACKS_GET = "https://ws.mcp3d.com/user/assets/packs";
        public static readonly string SALES_OFFER = "https://ws.mcp3d.com/sales/offers";
        public static readonly string SALES_INFO = "https://ws.mcp3d.com/sales/info";
        public static readonly string ASSETS_PETS = "https://ws.mcp3d.com/user/assets/pets";
        public static readonly string ASSETS_HISTORY = "https://ws.mcp3d.com/user/assets/history";
        public static readonly string ASSETS_CITIZENS = "https://ws.mcp3d.com/user/assets/citizens";
        public static readonly string CITIZEN_GET = "https://ws.mcp3d.com/citizen/get";
        public static readonly string REGIONS_LIST = "https://ws.mcp3d.com/regions/list";
        public static readonly string PERKS_DISTRICT = "https://ws.mcp3d.com/perks/districts";
        public static readonly string DISTRICT_INFO = "https://ws.mcp3d.com/newspaper/district/info";
        public static readonly string BALANCES = "https://ws.mcp3d.com/balances";
        public static readonly string MISSION = "https://ws.mcp3d.com/missions/land/";
        public static readonly string CONTRACT = "https://ws.mcp3d.com/contract";
    }

    public enum UPDATE_TYPE
    {
        FULL = 1,
        PARTIAL = 2,
        COPY_MASTER = 3,
    }

    public enum EVENT_TYPE
    {
        UNKNOWN = 0,       
        CREATED = 1,
        LISTED_ON_MARKET = 2,
        PURCHASED_ON_MARKET = 3,
        TRANSFERED_TO_NEW_OWNER = 4,
        UNITS_MINTED = 5,
        CUSTOM_BUILD = 6,
    }

    public enum BALANCE
    {
        SKIP = -1
    }

    public enum NETWORK
    {
        ETHEREUM_ID = 1,
        ROPSTEN_ID = 3,         // Ethereum Test network.
        POLYGON_ID = 137,       //'0x89',
        BINANCE_ID = 56,        // '0x38'
        BINANCE_TESTNET_ID = 97,   // '0x61'
        TRON_ID = -1            // chain id is actual 1 but eth is also matching so using -1 for internal distinction.
    };
}