import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';

const routes: Routes = [
  { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
  { path: 'suppliers', canActivate: [featureGuard('counterparties.suppliers')], loadComponent: () => import('./supplier-list/supplier-list.component') },
  { path: 'clients', canActivate: [featureGuard('counterparties.clients')], loadComponent: () => import('./client-list/client-list.component') },
  { path: ':id', loadComponent: () => import('./detail/counterparty-detail.component') }
];

export default routes;
