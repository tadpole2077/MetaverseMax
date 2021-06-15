import { Injectable } from '@angular/core';

interface OwnerAccount {
  matic_key: string;
  checked_matic_key: string;
  tron_key: string;
  name: string;
  checked: boolean;
}


@Injectable()
export class Globals {

  public ownerAccount: OwnerAccount;
  public windowTron: any;

  constructor() {
    
    this.ownerAccount = {
      matic_key : "Not Found",
      checked_matic_key : "",
      tron_key : "",
      name : "",
      checked : false
    }
  }

}


export {
  OwnerAccount  
}
