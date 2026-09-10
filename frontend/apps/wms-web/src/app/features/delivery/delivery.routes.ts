import type { Routes } from '@angular/router';

import { permissionGuard } from '../../core/auth/wms-guards';

const view = [permissionGuard('delivery.view')];

/**
 * `/delivery` — ota marshrutda `moduleGuard('DELIVERY')` + `permissionGuard('delivery.view')`.
 * `data.titleKey` — `i18n.spec` sarlavha kalitini tekshiradi.
 */
export const DELIVERY_ROUTES: Routes = [
  {
    path: '',
    canActivate: view,
    data: { titleKey: 'delivery.deliveries' },
    loadComponent: () => import('./deliveries/deliveries.component'),
  },
  {
    path: 'new',
    canActivate: [permissionGuard('delivery.manage')],
    data: { titleKey: 'delivery.deliveries' },
    loadComponent: () => import('./delivery-create/delivery-create.component'),
  },
  {
    path: 'vehicles',
    canActivate: view,
    data: { titleKey: 'delivery.vehicles' },
    loadComponent: () => import('./vehicles/vehicles.component'),
  },
  {
    path: 'drivers',
    canActivate: view,
    data: { titleKey: 'delivery.drivers' },
    loadComponent: () => import('./drivers/drivers.component'),
  },
  {
    path: ':id',
    canActivate: view,
    data: { titleKey: 'delivery.deliveries' },
    loadComponent: () => import('./delivery-detail/delivery-detail.component'),
  },
];
