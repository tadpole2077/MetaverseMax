import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './app.component';
import { AccountApproveComponent } from './account-approve/account-approve.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { NavMenuWorldComponent } from './nav-menu-world/nav-menu-world.component';
import { HomeComponent } from './home/home.component';
import { SearchPlotComponent } from './search-plot/search-plot.component';
import { SearchDistrictComponent } from './search-district/search-district.component';
import { GraphTaxComponent } from './graph-tax/graph-tax.component';
import { GraphDamageComponent } from './graph-damage/graph-damage.component';
import { GraphFundComponent } from './graph-fund/graph-fund.component';
import { TaxChangeComponent } from './tax-change/tax-change.component';
import { NoteModalComponent } from './note-modal/note-modal.component';
import { PetModalComponent } from './pet-modal/pet-modal.component';
import { CitizenModalComponent } from './citizen-modal/citizen-modal.component';
import { CitizenBuildingTableComponent } from './citizen-building-table/citizen-building-table.component';
import { OfferModalComponent } from './offer-modal/offer-modal.component';
import { OwnerDataComponent } from './owner-data/owner-data.component';
import { ProdHistoryComponent } from './production-history/prod-history.component';
import { DistrictListComponent } from './district-list/district-list.component';
import { DistrictSummaryComponent } from './district-summary/district-summary.component';
import { DistrictNotificationComponent } from './district-notification/district-notification.component';
import { BuildingIPComponent } from './building-ip/building-ip.component';
import { BuildingFilterComponent } from './building-filter/building-filter.component';

import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { NumberDirective } from './numberonly.directive';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { ClipboardModule } from '@angular/cdk/clipboard';

import { Globals } from './common/global-var';


@NgModule({
  declarations: [
    AppComponent,
    AccountApproveComponent,
    NavMenuComponent,
    NavMenuWorldComponent,
    HomeComponent,
    SearchPlotComponent,
    SearchDistrictComponent,
    TaxChangeComponent,
    GraphTaxComponent,
    GraphFundComponent,
    GraphDamageComponent,
    OwnerDataComponent,
    NoteModalComponent,
    PetModalComponent,
    CitizenModalComponent,
    CitizenBuildingTableComponent,
    OfferModalComponent,
    ProdHistoryComponent,
    DistrictListComponent,
    DistrictSummaryComponent,
    DistrictNotificationComponent,
    BuildingIPComponent,
    BuildingFilterComponent,
    NumberDirective  
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatExpansionModule,
    MatIconModule,
    MatCheckboxModule,
    MatTabsModule,
    MatButtonToggleModule,
    MatButtonModule,
    MatBadgeModule,
    DragDropModule,
    NgbDropdownModule,
    NgxChartsModule,    
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'bnb', component: HomeComponent, pathMatch: 'full' },
      { path: 'trx', component: HomeComponent, pathMatch: 'full' },
      { path: 'eth', component: HomeComponent, pathMatch: 'full' },

      { path: 'owner-data', component: OwnerDataComponent },
      { path: 'bnb/owner-data', component: OwnerDataComponent },
      { path: 'trx/owner-data', component: OwnerDataComponent },
      { path: 'eth/owner-data', component: OwnerDataComponent },

      { path: 'district-list', component: DistrictListComponent },
      { path: 'bnb/district-list', component: DistrictListComponent },
      { path: 'trx/district-list', component: DistrictListComponent },
      { path: 'eth/district-list', component: DistrictListComponent },

      { path: 'district-summary', component: DistrictSummaryComponent },
      { path: 'bnb/district-summary', component: DistrictSummaryComponent },
      { path: 'trx/district-summary', component: DistrictSummaryComponent },
      { path: 'eth/district-summary', component: DistrictSummaryComponent },

      { path: 'district-notification', component: DistrictNotificationComponent },
      { path: 'bnb/district-notification', component: DistrictNotificationComponent },
      { path: 'trx/district-notification', component: DistrictNotificationComponent },
      { path: 'eth/district-notification', component: DistrictNotificationComponent },

      { path: 'building-ip', component: BuildingIPComponent },
      { path: 'bnb/building-ip', component: BuildingIPComponent },
      { path: 'trx/building-ip', component: BuildingIPComponent },
      { path: 'eth/building-ip', component: BuildingIPComponent }
    ]),
    BrowserAnimationsModule,
    ClipboardModule
  ],
  providers: [ Globals ],
  bootstrap: [AppComponent]
})
export class AppModule { }
