import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./settings.component') }
];

export default routes;
