import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject, ViewChild, Input, NgZone } from '@angular/core';
import { NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';
import { Globals, WORLD } from '../common/global-var';
import { ThemePalette } from '@angular/material/core';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';


interface WorldNameCollection {
  world_name: WorldName[];
}
interface WorldName {
  id: number;
  name: string;
}

@Component({
  selector: 'app-nav-menu-owner',
  templateUrl: './nav-menu-owner.component.html',
  styleUrls: ['./nav-menu-owner.component.css']
})
export class NavMenuOwnerComponent {

  color: ThemePalette = 'accent';
  checked = false;
  hoverIcon = false;
  hoverDropdown = false;

  httpClient: HttpClient;
  baseUrl: string;
  public worldNamelist: WorldName[];
  public selectedWorldName: string = "World: Tron";

  @ViewChild(NgbDropdown, { static: true }) ownerDropdown: NgbDropdown;
  @Output() darkModeChangeEvent = new EventEmitter<any>();

  constructor(private zone: NgZone, public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') public rootBaseUrl: string) {

    this.httpClient = http;
    this.baseUrl = rootBaseUrl + "api/" + globals.worldCode;
    
  }

  // to avoid a "Navigation triggered outside Angular zone" error, due to newly rendered links from wallet site link, need to run navigation within zone.run()
  navMyPortfolio() {
    this.zone.run(() => {
      this.router.navigate(['/', this.globals.worldCode, 'owner-data'], { queryParams: { matic: 'myportfolio' }, });
    });
  }

  darkModeChange(eventSlider: MatSlideToggleChange) {

    // needs to be updated again - due to possible change in world since component was initially loaded.
    this.baseUrl = this.rootBaseUrl + "api/" + this.globals.worldCode; 

    // bubble event up to parent component to trigger theme change
    this.darkModeChangeEvent.emit(eventSlider.checked);    

    // update db - WS call    
    this.updateDarkMode(this.globals.ownerAccount.matic_key, eventSlider.checked);
    
    this.globals.ownerAccount.dark_mode = eventSlider.checked;
  }


  updateDarkMode(maticKey: string, darkMode: boolean) {

    let params = new HttpParams();
    params = params.append('owner_matic_key', maticKey);
    params = params.append('dark_mode', darkMode ? 'true':'false');

    if (this.globals.ownerAccount.wallet_active_in_world) {

      this.httpClient.get<Object>(this.baseUrl + '/OwnerData/SetDarkMode', { params: params })
        .subscribe({
          next: (result) => {
          },
          error: (error) => { console.error(error) }
        });
    }

    return;
  }

  onHoverShow(event, controlId: number): void {
    this.ownerDropdown.open();
    this.hoverIcon = controlId == 1;
    this.hoverDropdown = controlId == 2;
  }
  onHoverHide(event, callingControl): void {

    if ((this.hoverIcon == false && callingControl == 2) || (this.hoverDropdown == false && callingControl == 1) ) {
      this.ownerDropdown.close();
    }
  }
}
