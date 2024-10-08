enum ALERT_ACTION {  
  ADD = 1,
  REMOVE = 2,
}

enum ALERT_TYPE {
  INITIAL_LAND_VALUE = 1,
  CONSTRUCTION_TAX = 2,
  PRODUCTION_TAX = 3,
  DISTRIBUTION = 4,
  BUILDING_RANKING = 5,
  NEW_BUILDING = 6,
}

enum ALERT_ICON_TYPE {
  INFO = 1,
  TAX = 2,
  STAMINA = 3,
  NEW_OFFER = 4,
  RANKING = 5,
  NEW_BUILDING = 6,
}

const STATUS = {
    SUCCESS : 'success'
};

enum ICON_TYPE_CHANGE {
  NONE = 0,
  INCREASE = 1,
  DECREASE = 2
}

enum PENDING_ALERT {
  ALL = 1,
  UNREAD = 2,
}

enum MIN_STAMINA
{
    ENERGY = 15,
    RESIDENTIAL = 100,
    OFFICE = 25,
    COMMERCIAL = 5,
    MUNICIPAL = 10,
    INDUSTRIAL = 25,
    PRODUCTION = 50
}

// Use BUILDING_TYPE[typeId] - return a string matching id.
const BUILDING_TYPE = {
    1: 'Empty Plot',
    2: 'Residential',
    3: 'Energy',
    4: 'Commercial',
    5: 'Industry',
    6: 'Office',
    7: 'Production',
    8: 'Municipal',
    10: 'Parcel',
    100: 'AOI'
};

enum BUILDING {
  NO_FILTER = -1,
  EMPTYPLOT = 0,
  RESIDENTIAL = 1,
  ENERGY = 3,
  COMMERCIAL = 4,
  INDUSTRIAL = 5,
  OFFICE = 6,
  PRODUCTION = 7,
  MUNICIPAL = 8,
  PARCEL = 10,
  AOI = 100
}

enum BUILDING_SUBTYPE {
  NOT_FOUND = -1,
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

enum PRODUCT_IMG {
  WOOD = 'wood.png',
  SAND = 'sand.png',
  STONE = 'stone.png',
  METAL = 'metal.png',
  BRICK = 'brick.png',
  GLASS = 'glass.png',
  WATER = 'water.png',
  ENERGY = 'energy.png',
  STEEL = 'steel.png',
  CONCRETE = 'concrete.png',
  PLASTIC = 'plastic.png',
  GLUE = 'glue.png',
  MIXES = 'mixes.png',
  COMPOSITE = 'composite.png',
  PAPER = 'paper.png',
}

const PRODUCT = {
    WOOD: 1,
    SAND: 2,
    STONE: 3,
    METAL: 4,
    BRICK: 5,
    GLASS: 6,
    WATER: 7,
    ENERGY: 8,
    STEEL: 9,
    CONCRETE: 10,
    PLASTIC: 11,
    GLUE: 12,
    MIXES: 13,
    COMPOSITE: 14,
    PAPER: 15,
    FACTORY_PRODUCT: 99
};
// Use: PRODUCT_NAME[productId] >> returns a string of the product name.
const PRODUCT_NAME = {
    1: 'Wood',
    2: 'Sand',
    3: 'Stone',
    4: 'Metal',
    5: 'Brick',
    6: 'Glass',
    7: 'Water',
    8: 'Energy',
    9: 'Steel',
    10: 'Concrete',
    11: 'Plastic',
    12: 'Glue',
    13: 'Mixes',
    14: 'Composite',
    15: 'Paper',
    99: 'Factory Product',
};

const FACTORY_PRODUCT = {
    WHITE_AIR_CON: 1,
    CCTV_WHITE: 2,
    RED_FIRE_ALARM: 3,
    ROUTER_BLACK: 4,
    WHITE_SAT: 5,
    GREEN_SAT: 6,
    RED_SAT: 7,
    GREEN_AIR_CON: 8,
    RED_AIR_CON: 9,
    CCTV_RED: 10
};

const FACTORY_PRODUCT_NAME = {
    1: 'White Air Con',
    2: 'CCTV White',
    3: 'Red Fire Alarm',
    4: 'Router Black',
    5: 'White Sat',
    6: 'Green Sat',
    7: 'Red Sat',
    8: 'Green Air Con',
    9: 'Red Air Con',
    10: 'CCTV Red'
};
const FACTORY_PRODUCT_IMG = {
    1: 'white_ac.png',
    2: 'white_cctv.png',
    3: 'red_alarm.png',
    4: 'black_router.png',
    5: 'white_sat.png',
    6: 'green_sat.png',
    7: 'red_sat.png',
    8: 'green_ac.png',
    9: 'red_ac.png',
    10: 'red_cctv.png'
};

// use TRANSACTION_STATUS.PENDING , converts to number.
const TRANSACTION_STATUS = {
    PENDING: 1,
    COMPLETED: 2
};

enum MCP_CONTRACT {
  MEGA_BANK_ETHEREUM = '0x82613a289B48f3012339E3f2ADaaA57568F1bF70',
  DEPOSIT_ETHEREUM = '0x3F0806a8760f824ae2fE4961ac3a64eEa8Be7B25',
  MW_ETHEREUM ='0x1892F6ff5FBE11c31158F8c6f6F6e33106c5B10E',

  MEGA_BANK_BSC = '0x29E4590b970ca60de81BC8968759DbC9E98dB031',
  DEPOSIT_BSC = '0xD96c0110E6cE40787602ec9038d66E7277E5c61d',
  MW_BSC = '0x0af8c016620d3ed0c56381060e8Ab2917775885e',

  MEGA_BANK_TRON = '410801a98f0350fd670b01be543abadfc4fded11d1',
  DEPOSIT_TRON = '41e261c3dbc010c890a474f3bc384a23d8f0f629d7'
}
enum MCP_CONTRACT_NAME{
  MEGA_BANK_ETHEREUM = 'MegaBankEthereum',
  DEPOSIT_ETHEREUM = 'DepositEthereum',
  MW_ETHEREUM = 'MWEthereum',

  MEGA_BANK_BSC = 'MegaBankBsc',
  DEPOSIT_BSC = 'DepositBsc',
  MW_BSC = 'MWBsc',

  MEGA_BANK_TRON = 'MegaBankTron',
  DEPOSIT_TRON = 'DepositTron'
}

const CUSTOM_BUILDING_CATEGORY = {
    0: 'Parcel',
    1: 'Downtown',
    2: 'Housing',
    3: 'Shopping',
    4: 'Retreat',
    5: 'Eco',
    6: 'Luxury',
    7: 'Midtown',
    8: 'Headquarters',
    9: 'Countryside',
    10: 'Rural',
    100: 'Main Tower'
};

const EVENT_TYPE =
{
    0: 'Unknown event',       
    1: 'Created',
    2: 'Listed On Market',
    3: 'Purchased On Market', 
    4: 'New Owner Transfer',
    5: 'Minted Units',
    6: 'Custom Built',
};


// Blockchain
const BLOCKCHAIN = {
    ETHEREUM: 1,
    TRON: 2,
    BNB: 3,
    POLYGON: 4
};

const TRANSACTION_TYPE = {
    TRANSFER: 1,
    DEPOSIT: 2
};

enum HEX_NETWORK {
  APP_SELECTED = '',
  ETHEREUM_ID = '0x1',
  ROPSTEN_ID = '0x3',       // Ethereum Test network.
  POLYGON_ID = '0x89',
  BINANCE_ID = '0x38',      // 56 decimal
  BINANCE_TESTNET_ID = '0x61',
  TRON = '',
}

const NETWORKS_DESC = {
    1: 'Ethereum Main Network',
    3: 'Ropsten Test Network',
    4: 'Rinkeby Test Network',
    5: 'Goerli Test Network',
    42: 'Kovan Test Network',
    56: 'Binance Smart Chain',
    1337: 'Ganache',
};

enum METAMASK_ERROR_CODE {
  UNRECOGNISED_CHAIN = 4902, 
}



export {
    ALERT_ACTION,
    ALERT_TYPE,
    ALERT_ICON_TYPE,
    BLOCKCHAIN,
    BUILDING_TYPE,
    BUILDING_SUBTYPE,
    BUILDING,
    CUSTOM_BUILDING_CATEGORY,
    EVENT_TYPE,
    HEX_NETWORK,
    ICON_TYPE_CHANGE,
    METAMASK_ERROR_CODE,  
    MCP_CONTRACT,
    MCP_CONTRACT_NAME,
    MIN_STAMINA,
    NETWORKS_DESC,
    PENDING_ALERT, 
    PRODUCT_IMG,
    PRODUCT_NAME,
    FACTORY_PRODUCT,
    FACTORY_PRODUCT_IMG,
    PRODUCT,
    STATUS,
    TRANSACTION_STATUS,      
    TRANSACTION_TYPE
};
