import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';

const routes: Routes = [
  { path: '', loadComponent: () => import('./summary/finance-summary.component') },
  { path: 'transactions', canActivate: [featureGuard('finance.transactions')], loadComponent: () => import('./transactions/transactions.component') },
  { path: 'debts', canActivate: [featureGuard('finance.debts')], loadComponent: () => import('./debts/debts.component') },
  { path: 'payments', canActivate: [featureGuard('finance.payments')], loadComponent: () => import('./payments/payments.component') }
];

export default routes;
