import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';

const routes: Routes = [
  { path: '', loadComponent: () => import('./stock-overview/stock-overview.component') },
  { path: 'warehouses', loadComponent: () => import('./warehouse-list/warehouse-list.component') },
  { path: 'movements', loadComponent: () => import('./movements/movements.component') },
  { path: 'locations', canActivate: [featureGuard('warehouse.locations')], loadComponent: () => import('./locations/locations.component') },
  { path: 'batches', canActivate: [featureGuard('warehouse.batches')], loadComponent: () => import('./batches/batches.component') }
];

export default routes;
