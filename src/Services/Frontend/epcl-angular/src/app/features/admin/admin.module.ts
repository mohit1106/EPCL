import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';

import { AdminDashboardComponent } from './pages/dashboard/dashboard.component';
import { AdminUsersComponent } from './pages/users/users.component';
import { AdminStationsComponent } from './pages/stations/stations.component';
import { AdminPricesComponent } from './pages/prices/prices.component';
import { AdminFraudComponent } from './pages/fraud/fraud.component';
import { AdminAuditComponent } from './pages/audit/audit.component';
import { AdminReportsComponent } from './pages/reports/reports.component';
import { SystemHealthComponent } from './pages/system-health/system-health.component';
import { AdminDocumentsComponent } from './pages/documents/documents.component';
import { AdminHelpRequestsComponent } from './pages/help-requests/help-requests.component';
import { AdminReplenishmentRequestsComponent } from './pages/replenishment-requests/replenishment-requests.component';
import { AdminDriversComponent } from './pages/drivers/drivers.component';
import { AdminShiftHistoryComponent } from './pages/shift-history/shift-history.component';

const routes: Routes = [
  { path: 'dashboard', component: AdminDashboardComponent },
  { path: 'users', component: AdminUsersComponent },
  { path: 'stations', component: AdminStationsComponent },
  { path: 'prices', component: AdminPricesComponent },
  { path: 'fraud', component: AdminFraudComponent },
  { path: 'audit', component: AdminAuditComponent },
  { path: 'reports', component: AdminReportsComponent },
  { path: 'system-health', component: SystemHealthComponent },
  { path: 'documents', component: AdminDocumentsComponent },
  { path: 'help-requests', component: AdminHelpRequestsComponent },
  { path: 'replenishment-requests', component: AdminReplenishmentRequestsComponent },
  { path: 'drivers', component: AdminDriversComponent },
  { path: 'shift-history', component: AdminShiftHistoryComponent },
];

@NgModule({
  declarations: [
    AdminDashboardComponent,
    AdminUsersComponent,
    AdminStationsComponent,
    AdminPricesComponent,
    AdminFraudComponent,
    AdminAuditComponent,
    AdminReportsComponent,
    SystemHealthComponent,
    AdminDocumentsComponent,
    AdminHelpRequestsComponent,
    AdminReplenishmentRequestsComponent,
    AdminDriversComponent,
    AdminShiftHistoryComponent,
  ],
  imports: [SharedModule, FormsModule, BaseChartDirective, RouterModule.forChild(routes)],
})
export class AdminModule {}
