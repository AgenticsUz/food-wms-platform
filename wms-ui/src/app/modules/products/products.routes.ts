import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./products.component') }
];

export default routes;
