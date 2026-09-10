import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

const stages = { canActivate: [featureGuard('production.stages')] };
const recipes = { canActivate: [featureGuard('production.recipes')] };
const orders = { canActivate: [featureGuard('production.orders')] };

/**
 * `/production` — ota marshrutda `moduleGuard('PRODUCTION')` + `permissionGuard('production.view')`.
 *
 * `data.titleKey` ko'chirilgandan keyin ham qoldirildi: `i18n.spec.ts` har marshrut
 * sarlavha kalitining tarjimasini tekshiradi — kalit shu yerda bo'lsa, qorovul ekran
 * ko'chgandan keyin ham ishlayveradi.
 */
export const PRODUCTION_ROUTES: Routes = [
  { path: '', redirectTo: 'orders', pathMatch: 'full' },
  {
    path: 'stages',
    ...stages,
    data: { titleKey: 'production.stages' },
    loadComponent: () => import('./stages/stages.component'),
  },
  {
    path: 'recipes',
    ...recipes,
    data: { titleKey: 'production.recipes' },
    loadComponent: () => import('./recipe-list/recipe-list.component'),
  },
  {
    path: 'recipes/new',
    ...recipes,
    data: { titleKey: 'production.recipes' },
    loadComponent: () => import('./recipe-create/recipe-create.component'),
  },
  {
    path: 'recipes/:id',
    ...recipes,
    data: { titleKey: 'production.recipes' },
    loadComponent: () => import('./recipe-detail/recipe-detail.component'),
  },
  {
    path: 'orders',
    ...orders,
    data: { titleKey: 'production.orders' },
    loadComponent: () => import('./order-list/order-list.component'),
  },
  {
    path: 'orders/new',
    ...orders,
    data: { titleKey: 'production.orders' },
    loadComponent: () => import('./order-create/order-create.component'),
  },
  {
    path: 'orders/:id',
    ...orders,
    data: { titleKey: 'production.orders' },
    loadComponent: () => import('./order-detail/order-detail.component'),
  },
];
