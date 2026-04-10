import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./finance.component') }
];

export default routes;
