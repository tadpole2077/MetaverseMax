import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './app.component';
import { AccountApproveComponent } from './account-approve/account-approve.component';
import { AlertMenuComponent } from './alert-menu/alert-menu.component';
import { AlertBottomComponent } from './alert-bottom/alert-bottom.component';
import { AlertHistoryComponent } from './alert-history/alert-history.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { NavMenuWorldComponent } from './nav-menu-world/nav-menu-world.component';
import { NavMenuOwnerComponent } from './nav-menu-owner/nav-menu-owner.component';
import { HomeComponent } from './home/home.component';
import { SearchPlotComponent } from './search-plot/search-plot.component';
import { SearchDistrictComponent } from './search-district/search-district.component';
import { GraphTaxComponent } from './graph-tax/graph-tax.component';
import { GraphDamageComponent } from './graph-damage/graph-damage.component';
import { GraphFundComponent } from './graph-fund/graph-fund.component';
import { TaxChangeComponent } from './tax-change/tax-change.component';
import { NoteModalComponent } from './note-modal/note-modal.component';
import { PetModalComponent } from './pet-modal/pet-modal.component';
import { PackModalComponent } from './pack-modal/pack-modal.component';
import { CitizenModalComponent } from './citizen-modal/citizen-modal.component';
import { CitizenBuildingTableComponent } from './citizen-building-table/citizen-building-table.component';
import { OfferModalComponent } from './offer-modal/offer-modal.component';
import { OwnerDataComponent } from './owner-data/owner-data.component';
import { ProdHistoryComponent } from './production-history/prod-history.component';
import { PlayerMenuComponent } from './player-menu/player-menu.component';
import { DistrictListComponent } from './district-list/district-list.component';
import { DistrictSummaryComponent } from './district-summary/district-summary.component';
import { DistrictNotificationComponent } from './district-notification/district-notification.component';
import { BuildingIPComponent } from './building-ip/building-ip.component';
import { BuildingFilterComponent } from './building-filter/building-filter.component';
import { TransferAssetComponent } from './transfer-asset/transfer-asset.component';
import { WorldComponent } from './world/world.component';
import { CustomBuildingComponent } from './custom-building/custom-building.component';
import { CustomBuildingTableComponent } from './custom-building-table/custom-building-table.component';
import { MissionDeskComponent } from './mission-desk/mission-desk.component';
import { TabContainerLazyComponent } from './tab-container-lazy/tab-container-lazy.component';
import { TESTBankManageComponent } from './bank-manage/bank-manage.component';
import { BalanceComponent } from './balance/balance.component';
import { BalanceLogComponent } from './balance-log/balance-log.component';
import { BalanceManageDialogComponent } from './balance-manage-dialog/balance-manage-dialog.component';
import { DirectDepositDialogComponent } from './direct-deposit-dialog/direct-deposit-dialog.component';

import { ClipboardModule } from '@angular/cdk/clipboard';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { NumberDirective } from './directive/numberonly.directive';
import { NumberDecimalDirective } from './directive/number-decimal-only.directive';
import { TabExtractedBodyDirective } from './directive/tab-extracted-body.directive';
import { ImageFallbackDirective } from './directive/image-fallback.directive';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxChartsModule } from '@swimlane/ngx-charts';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatTableModule} from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatFormFieldModule, MAT_FORM_FIELD_DEFAULT_OPTIONS} from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatBottomSheetModule } from '@angular/material/bottom-sheet';
import { MatInputModule} from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule} from '@angular/material/tabs';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatBadgeModule } from '@angular/material/badge';
import { MatListModule } from '@angular/material/list';
import { MatDialogModule } from '@angular/material/dialog';

import { Globals } from './common/global-var';
import { Alert } from './common/alert';


@NgModule({
  declarations: [
    AppComponent,
    AccountApproveComponent,
    AlertMenuComponent,
    AlertBottomComponent,
    AlertHistoryComponent,
    NavMenuComponent,
    NavMenuWorldComponent,
    NavMenuOwnerComponent,
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
    PackModalComponent,
    TransferAssetComponent,
    CitizenModalComponent,
    CitizenBuildingTableComponent,
    OfferModalComponent,
    ProdHistoryComponent,
    PlayerMenuComponent,
    DistrictListComponent,
    DistrictSummaryComponent,
    DistrictNotificationComponent,
    BuildingIPComponent,
    BuildingFilterComponent,
    WorldComponent,
    CustomBuildingComponent,
    CustomBuildingTableComponent,
    MissionDeskComponent,
    NumberDirective,
    NumberDecimalDirective,
    TabExtractedBodyDirective,
    ImageFallbackDirective,
    TabContainerLazyComponent,
    TESTBankManageComponent,
    BalanceComponent,
    BalanceLogComponent,
    BalanceManageDialogComponent,
    DirectDepositDialogComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatExpansionModule,
    MatIconModule,
    MatCheckboxModule,
    MatTabsModule,
    MatButtonToggleModule,
    MatButtonModule,
    MatBadgeModule,
    MatBottomSheetModule,
    MatSlideToggleModule,
    MatListModule,
    MatDialogModule,
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
      { path: 'eth/building-ip', component: BuildingIPComponent },

      { path: 'world', component: WorldComponent },
      { path: 'bnb/world', component: WorldComponent },
      { path: 'trx/world', component: WorldComponent },
      { path: 'eth/world', component: WorldComponent },

      { path: 'bank-manage', component: TESTBankManageComponent },
      { path: 'bnb/bank-manage', component: TESTBankManageComponent  },
      { path: 'trx/bank-manage', component: TESTBankManageComponent  },
      { path: 'eth/bank-manage', component: TESTBankManageComponent  }
    ]),
    BrowserAnimationsModule,
    ClipboardModule
  ],
  providers: [
    Globals,
    Alert,
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
