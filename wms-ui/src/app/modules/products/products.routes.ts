import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./product-list/product-list.component') },
  { path: 'categories', loadComponent: () => import('./categories/categories.component') },
  { path: 'units', loadComponent: () => import('./units/units.component') }
];

export default routes;
