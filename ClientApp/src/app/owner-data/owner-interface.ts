
// Service Interfaces
interface OwnerLandData
{
  district_id: number;
  pos_x: number;
  pos_y: number;
  building_type: number;
  building_desc: string;
  building_img: string;
  last_action: string;
  plot_ip: number;
  ip_info: number;
  ip_bonus: number;
  token_id: number;
  building_level: number;
  resource: number;
  citizen_count: number;
  citizen_url: string;
  citizen_stamina: number;
  citizen_stamina_alert: boolean;
  forsale: boolean;
  forsale_price: number;
  alert: boolean;
  rented: boolean;
  current_influence_rank: number;
  condition: number;
}

interface OwnerData
{
  owner_name: string;
  owner_url: string;
  owner_matic_key: string;
  last_action: string;
  registered_date: string;
  last_visit: string;
  plot_count: number;
  developed_plots: number;
  plots_for_sale: number;
  stamina_alert_count: number;
  offer_count: number;
  offer_sold_count: number;
  owner_offer: Offer[];
  owner_offer_sold: Offer[];
  offer_last_updated: string;
  pet_count: number;
  citizen_count: number;
  district_plots: DistrictPlot[];
  owner_land: OwnerLandData[];
  search_info: string;
}

interface Offer
{
  buyer_matic_key: string;
  buyer_owner_name: string;
  buyer_avatar_id: number;
  buyer_offer: number;
  offer_date: string;
  token_id: number;
  token_type_id: number;
  token_type: string;
  token_district: number;
  token_pos_x: number;
  token_pos_y: number;
}


interface PlotPosition
{
  plotX: string,
  plotY: string,
  rotateEle: Element
}

interface DistrictPlot
{
  district: number[];
}

interface Pet {
  token_id: number;
  name: string;
  trait: string;
  level: number;  
}

interface PortfolioPet {
  pet_count: number;
  last_updated: string;
  pet: Pet[];
}

interface Citizen {
  token_id: number;
  name: string;
  generation: number;
  breeding: number;
  sex: string;
  trait_agility: number;
  trait_intelligence: number;
  trait_charisma: number;
  trait_endurance: number;
  trait_luck: number;
  trait_strength: number;
  trait_avg: number;
  max_stamina: number;
  on_sale: boolean;
  current_price: number;
  efficiency_industry: number;
  efficiency_production: number;
  efficiency_energy_water: number;
  efficiency_energy_electric: number;
  efficiency_office: number;
  efficiency_commercial: number;
  efficiency_municipal: number;

  building_img: string;
  building_desc: string;
  district_id: number;
  pos_x: number;
  pos_y: number;
  building_level: number;
  building: string;
}

interface PortfolioCitizen {
  last_updated: string;
  citizen: Citizen[];
}

interface FilterCount {
  empty: number;
  industry: number;
  production: number;
  energy: number;
  residential: number;
  office: number;
  commercial: number;
  municipal: number;
  poi: number;
}

const BUILDING = {
  NO_FILTER: -1,
  EMPTYPLOT: 0,
  RESIDENTIAL: 1,
  ENERGY: 3,
  COMMERCIAL: 4,
  INDUSTRIAL: 5,
  OFFICE: 6,
  PRODUCTION: 7,
  MUNICIPAL: 8,
  AOI: 100
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
  CONCRETE: 10 
}


export
{
  OwnerLandData,
  OwnerData,
  Offer,
  PlotPosition,
  DistrictPlot,
  Pet,
  PortfolioPet,
  Citizen,
  PortfolioCitizen,
  BUILDING,
  PRODUCT,
  FilterCount
}
