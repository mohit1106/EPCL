import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { FormsModule } from '@angular/forms';

import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { WalletComponent } from './pages/wallet/wallet.component';
import { PricesComponent } from './pages/prices/prices.component';
import { TransactionsComponent } from './pages/transactions/transactions.component';
import { StationsComponent } from './pages/stations/stations.component';
import { LoyaltyComponent } from './pages/loyalty/loyalty.component';
import { VehiclesComponent } from './pages/vehicles/vehicles.component';
import { ReferralComponent } from './pages/referral/referral.component';
import { PaymentRequestsComponent } from './pages/payment-requests/payment-requests.component';
import { ParkingComponent } from './pages/parking/parking.component';

const routes: Routes = [
  { path: 'dashboard', component: DashboardComponent },
  { path: 'wallet', component: WalletComponent },
  { path: 'payment-requests', component: PaymentRequestsComponent },
  { path: 'prices', component: PricesComponent },
  { path: 'transactions', component: TransactionsComponent },
  { path: 'stations', component: StationsComponent },
  { path: 'parking', component: ParkingComponent },
  { path: 'loyalty', component: LoyaltyComponent },
  { path: 'vehicles', component: VehiclesComponent },
  { path: 'referral', component: ReferralComponent },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];

@NgModule({
  declarations: [
    DashboardComponent,
    WalletComponent,
    PricesComponent,
    TransactionsComponent,
    StationsComponent,
    ParkingComponent,
    LoyaltyComponent,
    VehiclesComponent,
    ReferralComponent,
    PaymentRequestsComponent,
  ],
  imports: [
    SharedModule,
    FormsModule,
    RouterModule.forChild(routes),
  ],
})
export class CustomerModule {}
