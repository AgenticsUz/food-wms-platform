import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';

const routes: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  { path: 'stages', canActivate: [featureGuard('production.stages')], loadComponent: () => import('./stages/stages.component') },
  { path: 'recipes', canActivate: [featureGuard('production.recipes')], loadComponent: () => import('./recipe-list/recipe-list.component') },
  { path: 'recipes/new', canActivate: [featureGuard('production.recipes')], loadComponent: () => import('./recipe-create/recipe-create.component') },
  { path: 'recipes/:id', canActivate: [featureGuard('production.recipes')], loadComponent: () => import('./recipe-detail/recipe-detail.component') },
  { path: 'orders', canActivate: [featureGuard('production.orders')], loadComponent: () => import('./order-list/order-list.component') },
  { path: 'orders/new', canActivate: [featureGuard('production.orders')], loadComponent: () => import('./order-create/order-create.component') },
  { path: 'orders/:id', canActivate: [featureGuard('production.orders')], loadComponent: () => import('./order-detail/order-detail.component') }
];

export default routes;
