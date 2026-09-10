import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

/**
 * `/finance` — ota marshrutda `moduleGuard('FINANCE')` + `permissionGuard('finance.view')`.
 * `data.titleKey` qoldirildi — `i18n.spec.ts` sarlavha kalitlarini tekshiradi.
 */
export const FINANCE_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'finance.overview' },
    loadComponent: () => import('./summary/finance-summary.component'),
  },
  {
    path: 'transactions',
    canActivate: [featureGuard('finance.transactions')],
    data: { titleKey: 'finance.transactions' },
    loadComponent: () => import('./transactions/transactions.component'),
  },
  {
    path: 'debts',
    canActivate: [featureGuard('finance.debts')],
    data: { titleKey: 'finance.debts' },
    loadComponent: () => import('./debts/debts.component'),
  },
  {
    path: 'payments',
    canActivate: [featureGuard('finance.payments')],
    data: { titleKey: 'finance.payments' },
    loadComponent: () => import('./payments/payments.component'),
  },
];
