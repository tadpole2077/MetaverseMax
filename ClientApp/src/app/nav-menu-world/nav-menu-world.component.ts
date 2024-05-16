import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, Output, EventEmitter, Inject, ViewChild, Input } from '@angular/core';
import { NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';
import { Application, WORLD } from '../common/global-var';

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

  readonly WORLD = WORLD;     // Allow mark-up to use enum

  httpClient: HttpClient;
  baseUrl: string;
  public worldNamelist: WorldName[];
  public selectedWorldName: string = "World: Tron";
  private mobileView: boolean = false;

  @ViewChild(NgbDropdown, { static: true }) worldDropDown: NgbDropdown;
  @Output() selectWorldEvent = new EventEmitter<any>();
  @Input() selectedWorld: number;

  constructor(public globals: Application, public router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    this.httpClient = http;
    this.baseUrl = baseUrl + "api/" + globals.worldCode;

    this.loadWorldListDropDown();
    this.updateSelectedWorldList(globals.selectedWorld);

    // Mobile View - remove secondary columns
    if (this.width < 768) {
      this.mobileView = true;
    }
    
  }

  public get width() {
    return window.innerWidth;
  }

  loadWorldListDropDown() {

    let params = new HttpParams();
    //params = params.append('opened', 'true');

    this.httpClient.get<WorldNameCollection>(this.baseUrl + '/Plot/getWorldNames', { params: params })
      .subscribe({
        next: (result) => {

          this.worldNamelist = result.world_name;

          this.updateSelectedWorldList(this.selectedWorld);

        },
        error: (error) => { console.error(error) }
      });        


    return;
  }

  // called by parent function on page load
  updateSelectedWorldList(worldId: WORLD) {

    var worldName = (worldId == WORLD.TRON ? "Tron" : worldId == WORLD.BNB ? "BSC" : "ETH");
    this.selectedWorldName = (this.mobileView ? "" : "World: ") + worldName;
    
  }

  setWorldVar( worldId:number, worldName:string ) {

    this.selectedWorldName = (this.mobileView ? "" : "World: ") + worldName;
    this.globals.selectedWorld = worldId;

    this.selectWorldEvent.emit(worldId);    // bubble event up to parent component
  }

}
