import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./transfers.component') }
];

export default routes;
