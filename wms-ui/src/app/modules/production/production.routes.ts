import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./production.component') }
];

export default routes;
