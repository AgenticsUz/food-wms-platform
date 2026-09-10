import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

/**
 * `/warehouse` — ota marshrutda `moduleGuard('WAREHOUSE_RAW')` + `permissionGuard('warehouse.view')`.
 *
 * `data.titleKey` ko'chirilgandan keyin ham qoldirildi: ekran nomi kaliti
 * (menyudagi bilan bir xil) va `i18n.spec` uni tarjimada borligini tekshiradi.
 */
export const WAREHOUSE_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'warehouse.stockOverview' },
    loadComponent: () => import('./stock-overview/stock-overview.component'),
  },
  {
    path: 'warehouses',
    data: { titleKey: 'warehouse.warehouses' },
    loadComponent: () => import('./warehouse-list/warehouse-list.component'),
  },
  {
    path: 'movements',
    data: { titleKey: 'warehouse.movements' },
    loadComponent: () => import('./movements/movements.component'),
  },
  {
    path: 'locations',
    canActivate: [featureGuard('warehouse.locations')],
    data: { titleKey: 'warehouse.locations' },
    loadComponent: () => import('./locations/locations.component'),
  },
  {
    path: 'batches',
    canActivate: [featureGuard('warehouse.batches')],
    data: { titleKey: 'warehouse.batches' },
    loadComponent: () => import('./batches/batches.component'),
  },
];
