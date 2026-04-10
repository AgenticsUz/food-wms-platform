import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
  { path: 'suppliers', loadComponent: () => import('./supplier-list/supplier-list.component') },
  { path: 'clients', loadComponent: () => import('./client-list/client-list.component') },
  { path: ':id', loadComponent: () => import('./detail/counterparty-detail.component') }
];

export default routes;
