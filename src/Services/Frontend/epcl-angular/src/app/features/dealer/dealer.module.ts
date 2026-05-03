import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { FormsModule } from '@angular/forms';

import { DealerDashboardComponent } from './pages/dashboard/dashboard.component';
import { NewSaleComponent } from './pages/new-sale/new-sale.component';
import { SaleConfirmationComponent } from './pages/sale-confirmation/sale-confirmation.component';
import { DealerTransactionsComponent } from './pages/transactions/transactions.component';
import { InventoryComponent } from './pages/inventory/inventory.component';
import { ShiftComponent } from './pages/shift/shift.component';
import { ReplenishmentComponent } from './pages/replenishment/replenishment.component';
import { DealerReportsComponent } from './pages/reports/reports.component';
import { DealerContactAdminComponent } from './pages/contact-admin/contact-admin.component';
import { PendingOffloadsComponent } from './pages/pending-offloads/pending-offloads.component';
import { ParkingTicketsComponent } from './pages/parking-tickets/parking-tickets.component';

const routes: Routes = [
  { path: 'dashboard', component: DealerDashboardComponent },
  { path: 'sales/new', component: NewSaleComponent },
  { path: 'sales/confirmation/:id', component: SaleConfirmationComponent },
  { path: 'transactions', component: DealerTransactionsComponent },
  { path: 'inventory', component: InventoryComponent },
  { path: 'shift', component: ShiftComponent },
  { path: 'replenishment', component: ReplenishmentComponent },
  { path: 'pending-offloads', component: PendingOffloadsComponent },
  { path: 'parking', component: ParkingTicketsComponent },
  { path: 'reports', component: DealerReportsComponent },
  { path: 'contact-admin', component: DealerContactAdminComponent },
];

@NgModule({
  declarations: [
    DealerDashboardComponent,
    NewSaleComponent,
    SaleConfirmationComponent,
    DealerTransactionsComponent,
    InventoryComponent,
    ShiftComponent,
    ReplenishmentComponent,
    DealerReportsComponent,
    DealerContactAdminComponent,
    PendingOffloadsComponent,
    ParkingTicketsComponent,
  ],
  imports: [SharedModule, FormsModule, RouterModule.forChild(routes)],
})
export class DealerModule {}
