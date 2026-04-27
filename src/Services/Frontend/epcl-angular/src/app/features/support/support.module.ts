import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';

import { HelpCenterComponent } from './pages/help-center/help-center.component';

const routes: Routes = [
  { path: '', component: HelpCenterComponent },
];

@NgModule({
  declarations: [HelpCenterComponent],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class SupportModule {}
