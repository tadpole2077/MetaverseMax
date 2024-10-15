import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { HomeComponent } from './home/home.component';
import { OwnerDataComponent } from './owner-data/owner-data.component';
import { DistrictListComponent } from './district-list/district-list.component';
import { DistrictSummaryComponent } from './district-summary/district-summary.component';
import { DistrictNotificationComponent } from './district-notification/district-notification.component';
import { BuildingIPComponent } from './building-ip/building-ip.component';
import { WorldComponent } from './world/world.component';
import { TESTBankManageComponent } from './bank-manage/bank-manage.component';

@NgModule({
    declarations: [
    ],
    imports: [
        CommonModule,
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
            { path: 'eth/bank-manage', component: TESTBankManageComponent },

            { path: '**', component: HomeComponent },                       // Default Route if no match found - avoid 404
        ])
    ],
    exports: [RouterModule]
})
export class AppRoutingModule { }
