
// Service Interfaces

// Owner with inheritance of 2 child types.
interface IOwnerLandData extends ICoordinates, ILandType
{
  district_id: number;  
  building_category: number;
  building_desc: string;
  building_img: string;
  action_type: number;
  last_action: string;
  last_actionUx: number;
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
  active: number;
  unit: number;
  c_r: boolean;     // Ready to collect bool
  c_d: number;      // Days to collect int
  c_h: number;      // Hours to collect int
  c_m: number;      // minutes to collect
  product_id: number;
}

interface ILandType {
  building_type: number;
}
interface ICoordinates {
  pos_x: number;
  pos_y: number;
}


interface IOwnerData
{
  /**
   * Owner name may be empty - typically using owner_matic_key when empty.
   */
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
  owner_offer: IOffer[];
  owner_offer_sold: IOffer[];
  offer_last_updated: string;
  pet_count: number;
  citizen_count: number;
  district_plots: IDistrictPlot[];
  owner_land: IOwnerLandData[];
  search_info: string;
  search_token: number;
  pack_count: number;
  pack : IPack[]
}

interface IPack {
  pack_id: number;
  amount: number;
  product_id: number;
}
interface IOffer
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


interface IDistrictPlot
{
  district: number[];
}

interface IPet {
  token_id: number;
  name: string;
  trait: string;
  level: number;  
}

interface IPortfolioPet {
  pet_count: number;
  last_updated: string;
  pet: IPet[];
}

interface ICitizen {
  token_id: number;
  name: string;
  generation: number;
  breeding: number;
  sex: string;
  trait_agility: number;
  trait_agility_pet: number;
  trait_intelligence: number;
  trait_intelligence_pet: number;
  trait_charisma: number;
  trait_charisma_pet: number;
  trait_endurance: number;
  trait_endurance_pet: number;
  trait_luck: number;
  trait_luck_pet: number;
  trait_strength: number;
  trait_strength_pet: number;
  trait_avg: number;
  trait_avg_pet: number;
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

interface IPortfolioCitizen {
  last_updated: string;
  slowdown: number;
  citizen: ICitizen[];
}

/**
 * Count of each building type used for filter icons
 */
interface IFilterCount {
  empty: number;
  industry: number;
  production: number;
  energy: number;
  residential: number;
  office: number;
  commercial: number;
  municipal: number;
  poi: number;
  parcel: number;
}


/**
 * Owner Land type set
 */
export
{
    IOwnerLandData,
    IOwnerData,
    IOffer,
    ICoordinates,
    IDistrictPlot,
    IPet,
    IPortfolioPet,
    ICitizen,
    IPortfolioCitizen,
    IFilterCount,
    IPack,
    ILandType
};
