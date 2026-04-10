import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { path: 'stages', loadComponent: () => import('./stages/stages.component') },
  { path: 'recipes', loadComponent: () => import('./recipe-list/recipe-list.component') },
  { path: 'recipes/new', loadComponent: () => import('./recipe-create/recipe-create.component') },
  { path: 'recipes/:id', loadComponent: () => import('./recipe-detail/recipe-detail.component') },
  { path: 'orders', loadComponent: () => import('./order-list/order-list.component') },
  { path: 'orders/new', loadComponent: () => import('./order-create/order-create.component') },
  { path: 'orders/:id', loadComponent: () => import('./order-detail/order-detail.component') }
];

export default routes;
