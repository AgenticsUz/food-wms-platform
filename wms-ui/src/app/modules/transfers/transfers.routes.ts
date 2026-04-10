import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./transfer-list/transfer-list.component') },
  { path: 'new', loadComponent: () => import('./transfer-create/transfer-create.component') },
  { path: ':id', loadComponent: () => import('./transfer-detail/transfer-detail.component') }
];

export default routes;
