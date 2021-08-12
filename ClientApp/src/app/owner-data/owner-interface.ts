
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
  ip_bonus: number;
  token_id: number;
  building_level: number;
  citizen_count: number;
  citizen_url: string;
  citizen_stamina: number;
  citizen_stamina_alert: boolean;
  forsale: boolean;
  forsale_price: number;
  alert: boolean;
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
  pet_count: number;
  district_plots: DistrictPlot[];
  owner_land: OwnerLandData[];
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
  trait: string;
  level: number;
  name: string;
}
interface PortfolioPet {
  pet_count: number;
  pet: Pet[];
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


export
{
  OwnerLandData,
  OwnerData,
  Offer,
  PlotPosition,
  DistrictPlot,
  Pet,
  PortfolioPet,
  BUILDING
}