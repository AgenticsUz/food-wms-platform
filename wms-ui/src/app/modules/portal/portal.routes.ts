import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./portal-dashboard.component') }
];

export default routes;
