import { Component, Inject, ViewChild, EventEmitter, ElementRef, Output, ChangeDetectorRef } from '@angular/core';
import { Application, WORLD } from '../common/global-var';

interface ResourceActive {
  name: string;
  total: number;
  active: number;
  shutdown: number;
  building_id: number;
  building_img: string;
  building_name: string;
}

type BuildingFilterIcon = {
  building_id: number;
  building_img: string;
  building_name: string;
  total: number;
}

@Component({
    selector: 'app-building-filter',
    templateUrl: './building-filter.component.html',
    styleUrls: ['./building-filter.component.css']
})
export class BuildingFilterComponent {

  @Output() filterBuildingEvent = new EventEmitter<number[]>();
  public buildingIconArray: BuildingFilterIcon[];
  public buildingTypeActive: number[] = [];
  public processing = false;

  constructor(public globals: Application, private elem: ElementRef, private cdf: ChangeDetectorRef) {

      this.initFilterIcons();

  }

  initFilterIcons() {

      this.buildingIconArray = [];

  }

  loadIcons(activeBuildings: ResourceActive[]) {

      activeBuildings.forEach(building => {

          let existingIcon: BuildingFilterIcon = null;

          this.buildingIconArray.forEach(buildingIcon => {
              if (buildingIcon.building_id == building.building_id) {
                  existingIcon = building;
                  buildingIcon.total += building.total;
              }
          });

          // Create a new Building Icon if not already added to array.
          if (existingIcon == null) {
              this.buildingIconArray.push({
                  building_id: building.building_id,
                  building_img: building.building_img,
                  building_name: building.building_name,
                  total: building.total
              });
          }

      });

  }

  // Filter By Building Type
  filterTable(event, filterValue: number, buildingType: number) {
    
      const progressIcon = event.srcElement.closest('a').lastElementChild.lastElementChild;
      progressIcon.classList.add('rotate');

      if (this.processing) {
          return;
      }

      // 100ms needed to update UI with progressIcon rotation anime.
      setTimeout(() => {

          this.processing = true;

          // CHECK If filter is BuildingType and already active, then this click is to disable it
          if (event.srcElement.closest('div').classList.contains('activeFilter')) {

              event.srcElement.closest('div').classList.remove('activeFilter');

              // Remove buildingType from active array.
              if (this.buildingTypeActive.includes(buildingType)) {
                  this.buildingTypeActive.splice(this.buildingTypeActive.indexOf(buildingType), 1);
              }

              this.filterBuildingEvent.emit(this.buildingTypeActive);
          }
          else {
              event.srcElement.closest('div').classList.add('activeFilter');

              // Add buildingType to active array.
              if (!this.buildingTypeActive.includes(buildingType)) {
                  this.buildingTypeActive.push(buildingType);
              }

              this.filterBuildingEvent.emit(this.buildingTypeActive);

          }

          progressIcon.classList.remove('rotate');
          this.processing = false;
      }, 100);
  }
}




