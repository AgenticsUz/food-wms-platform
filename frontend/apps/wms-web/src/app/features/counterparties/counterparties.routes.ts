import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

/**
 * `/counterparties` — ota marshrutda `permissionGuard('partners.view')` (modulsiz).
 * `data.titleKey` qoldirildi — `i18n.spec.ts` sarlavha kalitlarini tekshiradi.
 */
export const COUNTERPARTIES_ROUTES: Routes = [
  { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
  {
    path: 'suppliers',
    canActivate: [featureGuard('counterparties.suppliers')],
    data: { titleKey: 'partners.suppliers' },
    loadComponent: () => import('./supplier-list/supplier-list.component'),
  },
  {
    path: 'clients',
    canActivate: [featureGuard('counterparties.clients')],
    data: { titleKey: 'partners.clients' },
    loadComponent: () => import('./client-list/client-list.component'),
  },
  {
    path: ':id',
    data: { titleKey: 'nav.counterparties' },
    loadComponent: () => import('./detail/counterparty-detail.component'),
  },
];
