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
enum ICON_TYPE_CHANGE {
  NONE = 0,
  INCREASE = 1,
  DECREASE = 2
}

enum PENDING_ALERT {
  ALL = 1,
  UNREAD = 2,
}

enum PRODUCT_IMG {
  WOOD = "wood.png",
  SAND = "sand.png",
  STONE = "stone.png",
  METAL = "metal.png",
  BRICK = "brick.png",
  GLASS = "glass.png",
  WATER = "water.png",
  ENERGY = "energy.png",
  STEEL = "steel.png",
  CONCRETE = "concrete.png",
  PLASTIC = "plastic.png",
  GLUE = "glue.png",
  MIXES = "mixes.png",
  COMPOSITE = "composite.png",
  PAPER = "paper.png",
}
const PRODUCT_NAME = {
  1: "Wood",
  2: "Sand",
  3: "Stone",
  4: "Metal",
  5: "Brick",
  6: "Glass",
  7: "Water",
  8: "Energy",
  9: "Steel",
  10: "Concrete",
  11: "Plastic",
  12: "Glue",
  13: "Mixes",
  14: "Composite",
  15: "Paper",
  99: "Factory Product",
}

const TRANSACTION_STATUS = {
  PENDING: 1,
  COMPLETED: 2
}

const BLOCKCHAIN = {
  ETHEREUM: 1,
  TRON: 2,
  BNB: 3,
  POLYGON: 4
}

const TRANSACTION_TYPE = {
  TRANSFER: 1,
  DEPOSIT: 2
}

const CUSTOM_BUILDING_CATEGORY = {
  0 : "Parcel",
  1 : "Downtown",
  2 : "Housing",
  3 : "Shopping",
  4 : "Retreat",
  5 : "Eco",
  6 : "Luxury",
  7 : "Midtown",
  8 : "Headquarters",
  9 : "Countryside",
  10: "Rural",
  100: "Main Tower"
}

const EVENT_TYPE =
{
  0: "Unknown event",       
  1: "Created",
  2: "Listed On Market",
  3: "Purchased On Market", 
  4: "New Owner Transfer",
  5: "Minted Units",
  6: "Custom Built",
}

export {
  ALERT_TYPE,
  ALERT_ICON_TYPE,
  EVENT_TYPE,
  ICON_TYPE_CHANGE,
  PENDING_ALERT,
  ALERT_ACTION,
  PRODUCT_IMG,
  PRODUCT_NAME,
  TRANSACTION_STATUS,
  BLOCKCHAIN,
  TRANSACTION_TYPE,
  CUSTOM_BUILDING_CATEGORY
}
