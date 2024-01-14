
interface Parcel {
  parcel_id: number;
  pos_x: number;
  pos_y: number;
  district_id: number;
  unit_count: number;
  owner_matic: string;
  owner_name: string;
  owner_avatar_id: number;
  forsale: boolean;
  forsale_price: number;
  last_action: string;
  action_type: number;
  last_actionUx: number;
  building_img: string;
  building_name: string;
  plot_count: number;
  building_category_id: number;
  unit_forsale_count: number;
  unit_price_high_mega: number;
  unit_price_low_mega: number;
  unit_price_high_coin: number;
  unit_price_low_coin: number;
  unit_sale_largest_size: number;
  unit_sale_smallest_size: number;
}

interface ParcelCollection {
  parcel_count: number;
  building_count: number;
  parcel_list: Parcel[];
}


interface Mission {
  token_id: number;
  pos_x: number;
  pos_y: number;
  district_id: number;
  owner_matic: string;
  owner_name: string;
  owner_avatar_id: number;
  building_id: number;
  building_level: number;
  building_type_id: number;
  building_img: string;

  completed: number;
  max: number;
  reward: number;
  reward_owner: number;
  last_updated: string;
  available: boolean;
  balance: number;

  last_refresh: number;
}

interface MissionCollection {
  mission_list: Mission[];
  mission_count: number;
  mission_reward: number;
  all_mission_count: number;
  all_mission_available_count: number;
  all_mission_reward: number;
  all_mission_available_reward: number;
  repeatable_daily_reward: number;
}

interface TransactionCollection {
  transaction_list: Transaction[];
}
interface Transaction {
  hash: string;
  action: string;
  amount: number;
  event_recorded_gmt: string;
}

export {
  Parcel,
  ParcelCollection,
  Mission,
  MissionCollection,
  Transaction,
  TransactionCollection
}
