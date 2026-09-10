import type { Routes } from '@angular/router';

/**
 * `/transfers` — ota marshrutda `moduleGuard('TRANSFERS')` + `permissionGuard('transfers.view')`.
 *
 * `new` `:id` dan OLDIN: aks holda «new» id deb o'qilardi. `data.titleKey`
 * qoldirildi — ekran nomi kaliti, `i18n.spec` uni tekshiradi.
 */
export const TRANSFERS_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'nav.transfers' },
    loadComponent: () => import('./transfer-list/transfer-list.component'),
  },
  {
    path: 'new',
    data: { titleKey: 'nav.transfers' },
    loadComponent: () => import('./transfer-create/transfer-create.component'),
  },
  {
    path: ':id',
    data: { titleKey: 'nav.transfers' },
    loadComponent: () => import('./transfer-detail/transfer-detail.component'),
  },
];
