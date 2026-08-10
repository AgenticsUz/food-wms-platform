import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./modules/login/login.component') },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./modules/dashboard/dashboard.component') },
      { path: 'tenants', loadComponent: () => import('./modules/tenants/tenants.component') },
      { path: 'leads', loadComponent: () => import('./modules/leads/leads.component') },
      { path: 'organizations', loadComponent: () => import('./modules/organizations/organizations.component') },
      { path: 'plans', loadComponent: () => import('./modules/plans/plans.component') },
      { path: 'audit', loadComponent: () => import('./modules/audit/audit.component') }
    ]
  },
  { path: '**', redirectTo: '' }
];
