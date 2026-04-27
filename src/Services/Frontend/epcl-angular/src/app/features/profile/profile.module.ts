import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

import { SettingsComponent } from './pages/settings/settings.component';
import { NotificationsComponent } from './pages/notifications/notifications.component';

const routes: Routes = [
  { path: 'settings', component: SettingsComponent },
  { path: 'notifications', component: NotificationsComponent },
];

@NgModule({
  declarations: [SettingsComponent, NotificationsComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class ProfileModule {}
