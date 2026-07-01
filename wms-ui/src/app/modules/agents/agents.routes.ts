import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./agent-list/agent-list.component') },
  { path: ':id', loadComponent: () => import('./agent-detail/agent-detail.component') }
];

export default routes;
