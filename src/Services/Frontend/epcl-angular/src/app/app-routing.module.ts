import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { NotFoundComponent, UnauthorizedComponent } from './pages/error/error-pages.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full',
  },

  // Auth Module (lazy loaded)
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
  },

  // Customer Module (lazy loaded, guarded)
  {
    path: 'customer',
    loadChildren: () => import('./features/customer/customer.module').then((m) => m.CustomerModule),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Customer'] },
  },

  // Dealer Module (lazy loaded, guarded)
  {
    path: 'dealer',
    loadChildren: () => import('./features/dealer/dealer.module').then((m) => m.DealerModule),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Dealer'] },
  },

  // Admin Module (lazy loaded, guarded)
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.module').then((m) => m.AdminModule),
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin', 'SuperAdmin'] },
  },

  // Profile (any authenticated role)
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/profile.module').then((m) => m.ProfileModule),
    canActivate: [AuthGuard],
  },

  // Support
  {
    path: 'support',
    loadChildren: () => import('./features/support/support.module').then((m) => m.SupportModule),
    canActivate: [AuthGuard],
  },

  // Error pages
  { path: 'not-found', component: NotFoundComponent },
  { path: 'unauthorized', component: UnauthorizedComponent },
  { path: '**', component: NotFoundComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
