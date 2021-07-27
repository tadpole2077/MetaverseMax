import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { SearchPlotComponent } from './search-plot/search-plot.component';
import { SearchDistrictComponent } from './search-district/search-district.component';
import { TaxGraphComponent } from './tax-graph/tax-graph.component';
import { FundGraphComponent } from './fund-graph/fund-graph.component';
import { NoteModalComponent } from './note-modal/note-modal.component';
import { OfferModalComponent } from './offer-modal/offer-modal.component';
import { OwnerDataComponent } from './owner-data/owner-data.component';
import { ProdHistoryComponent } from './production-history/prod-history.component';
import { DistrictListComponent } from './district-list/district-list.component';
import { DistrictSummaryComponent } from './district-summary/district-summary.component';
import { DistrictNotificationComponent } from './district-notification/district-notification.component';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule, MatInputModule, MatIconModule, MatCheckboxModule, MatTabsModule } from '@angular/material';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { NumberDirective } from './numberonly.directive';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxChartsModule } from '@swimlane/ngx-charts';

import { Globals } from './common/global-var';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    SearchPlotComponent,
    SearchDistrictComponent,
    TaxGraphComponent,
    FundGraphComponent,
    OwnerDataComponent,
    NoteModalComponent,
    OfferModalComponent,
    ProdHistoryComponent,
    DistrictListComponent,
    DistrictSummaryComponent,
    DistrictNotificationComponent,
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
    DragDropModule,
    NgbDropdownModule,
    NgxChartsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'owner-data', component: OwnerDataComponent },
      { path: 'district-list', component: DistrictListComponent },
      { path: 'district-summary', component: DistrictSummaryComponent },
      { path: 'district-notification', component: DistrictNotificationComponent }
    ]),
    BrowserAnimationsModule
  ],
  providers: [ Globals ],
  bootstrap: [AppComponent]
})
export class AppModule { }
