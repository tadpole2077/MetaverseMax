import { GraphData } from '../common/graph-interface';

// Service Interfaces
interface OwnerSummary {
  summary_id: number,
  district_id: number;
  owner_matic: string;
  owner_name: string;
  owner_avatar_id: number;
  owned_plots: number;
  energy_count: number;
  industry_count: number;
  residential_count: number;
  production_count: number;
  office_count: number;
  municipal_count: number;
  poi_count: number;
  empty_count: number;
  update_instance: string;
  commercial_count: number;
  new_owner: boolean;
  new_owner_month: boolean;
}

interface District {
  update_instance: number;
  last_updateFormated: string;  
  district_id: number;
  district_name: string;
  land_count: number;
  initial_land_price: number;
  building_count: number;
  plots_claimed: number;
  energy_count: number;
  industry_count: number;
  production_count: number;
  office_count: number;
  commercial_count: number;
  municipal_count: number;
  owner_name: string;
  owner_avatar_id: number;
  owner_url: string;
  owner_matic: string;
  active_from: string;
  energy_tax: number;
  production_tax: number;
  commercial_tax: number;
  citizens_tax: number;
  promotion: string;
  promotion_start: string;
  promotion_end: string;

  distribution_period: number;
  produceTax: GraphData;
  constructTax: GraphData;
  fundHistory: GraphData;
  distributeHistory: GraphData;

  perkSchema: PerkSchema[];
  districtPerk: DistrictPerk[];
}

interface PerkSchema {
  perk_id: number;
  perk_name: string;
  perk_desc: string;
  level_Symbol: string;
  level_values: number[];
  level_max: number;
}

interface DistrictPerk {
  perk_id: number;
  perk_level: number;
}

interface TaxChange {
  change_date: string;
  tax_type: string;
  change_desc: string;
  change_owner: string;
}

export { 
  OwnerSummary,
  District,
  TaxChange
}
