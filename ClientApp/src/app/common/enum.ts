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
}

enum ALERT_ICON_TYPE {
  INFO = 1,
  TAX = 2,
  STAMINA = 3,
  NEW_OFFER = 4,
  RANKING = 5,
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


export {
  ALERT_TYPE,
  ALERT_ICON_TYPE,
  ICON_TYPE_CHANGE,
  PENDING_ALERT,
  ALERT_ACTION,
  PRODUCT_IMG,
  PRODUCT_NAME
}
