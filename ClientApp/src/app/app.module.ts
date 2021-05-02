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
import { NoteModalComponent } from './note-modal/note-modal.component';
import { OwnerDataComponent } from './owner-data/owner-data.component';
import { ProdHistoryComponent } from './production-history/prod-history.component';
import { DistrictListComponent } from './district-list/district-list.component';
import { DistrictSummaryComponent } from './district-summary/district-summary.component';
import { DistrictNotificationComponent } from './district-notification/district-notification.component';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { NumberDirective } from './numberonly.directive';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    SearchPlotComponent,
    SearchDistrictComponent,
    OwnerDataComponent,
    NoteModalComponent,
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
    DragDropModule,
    NgbDropdownModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'owner-data', component: OwnerDataComponent },
      { path: 'district-list', component: DistrictListComponent },
      { path: 'district-summary', component: DistrictSummaryComponent },
      { path: 'district-notification', component: DistrictNotificationComponent }
    ]),
    BrowserAnimationsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
