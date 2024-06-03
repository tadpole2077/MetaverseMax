import { Injectable } from '@angular/core';
//import { USERS } from '../mocks/users';

@Injectable({
  providedIn: 'root'
})
export class OwnerDataFilterService {

  //constructor() { }

  // Generic filter function, can filter on any field within T.  Optimal generic fn descriptor as it exposes each parameter as a Generic that can be used in the fn.
  // keyof T : defines any property name of T
  // U : is the filter property NAME - any properity of T
  // V : is the filter property TYPE, must match type of U
  public async filterBy<T, U extends keyof T, V extends T[U]>(listData: T[], filterfieldName: U, filterValue: V): Promise<T[]> {
  //public async filterBy<T>(listData: T[], filterfieldName: keyof T, filterValue: T[keyof T]): Promise<T[]> {
  //public async filterBy<T, U extends keyof T>(listData: T[], filterValue: T[U]): Promise<T[]> {

    const filterData: T[] = [];

    listData.forEach(land => {
      if (land[filterfieldName] == filterValue) {
        filterData.push(land);
      }
    });

    return filterData;
  }

  public async filterByDistrict<T extends {district_id: K}, K>(listData: T[], filterValue: K): Promise<T[]> {

    const filterData: T[] = [];

    listData.forEach(land => {
      if (land.district_id == filterValue) {
        filterData.push(land);
      }
    });

    return filterData;
  }

  public async filterByBuildingType<T extends {building_type: K}, K>(listData: T[], filterValue: K): Promise<T[]> {

    const filterData: T[] = [];

    listData.forEach(land => {
      if (land.building_type == filterValue) {
        filterData.push(land);
      }
    });

    return filterData;
  }  

}
