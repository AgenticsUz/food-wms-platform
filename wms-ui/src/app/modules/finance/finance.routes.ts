import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./summary/finance-summary.component') },
  { path: 'transactions', loadComponent: () => import('./transactions/transactions.component') },
  { path: 'debts', loadComponent: () => import('./debts/debts.component') },
  { path: 'payments', loadComponent: () => import('./payments/payments.component') }
];

export default routes;
