import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject, ViewChild, Input } from '@angular/core';
import { NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';
import { Globals, WORLD } from '../common/global-var';

interface WorldNameCollection {
  world_name: WorldName[];
}
interface WorldName {
  id: number;
  name: string;
}

@Component({
  selector: 'app-nav-menu-world',
  templateUrl: './nav-menu-world.component.html',
  styleUrls: ['./nav-menu-world.component.css']
})
export class NavMenuWorldComponent {

  httpClient: HttpClient;
  baseUrl: string;
  public worldNamelist: WorldName[];
  public selectedWorldName: string = "World: Tron";

  @ViewChild(NgbDropdown, { static: true }) worldDropDown: NgbDropdown;
  @Output() selectWorldEvent = new EventEmitter<any>();
  @Input() selectedWorld: number;

  constructor(public globals: Globals, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.loadWorldListDropDown();
    
  }

  loadWorldListDropDown() {

    let params = new HttpParams();
    //params = params.append('opened', 'true');

    this.httpClient.get<WorldNameCollection>(this.baseUrl + '/Plot/getWorldNames', { params: params })
      .subscribe((result: WorldNameCollection) => {

        this.worldNamelist = result.world_name;

        this.updateSelectedWorldList(this.selectedWorld);
        //this.worldDropDown.
        //this.removeLinkHighlight();
        //plotPos.rotateEle.classList.remove("rotate");

      }, error => console.error(error));


    return;
  }

  // called by parent function on page load
  updateSelectedWorldList(worldId: number) {

    var worldName = (worldId == WORLD.TRON ? "Tron" : worldId == WORLD.BNB ? "BSC" : "Ethereum");
    this.selectedWorldName = "World: " + worldName;
    
  }

  setWorldVar( worldId:number, worldName:string ) {

    this.selectedWorldName = "World: " + worldName;
    this.globals.selectedWorld = worldId;

    this.selectWorldEvent.emit(worldId);    // bubble event up to parent component
  }

}
