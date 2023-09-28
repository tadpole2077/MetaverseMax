
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

export {
  Parcel,
  ParcelCollection
}
