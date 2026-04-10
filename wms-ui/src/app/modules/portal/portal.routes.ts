import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./dashboard/portal-dashboard.component') },
  { path: 'transfers', loadComponent: () => import('./transfers/portal-transfers.component') },
  { path: 'transfers/:id', loadComponent: () => import('./transfer-detail/portal-transfer-detail.component') },
  { path: 'finance', loadComponent: () => import('./finance/portal-finance.component') }
];

export default routes;
