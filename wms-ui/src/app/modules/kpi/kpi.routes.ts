import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./kpi.component') }
];

export default routes;
