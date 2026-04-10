import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./stock-overview/stock-overview.component') },
  { path: 'movements', loadComponent: () => import('./movements/movements.component') },
  { path: 'locations', loadComponent: () => import('./locations/locations.component') },
  { path: 'batches', loadComponent: () => import('./batches/batches.component') }
];

export default routes;
