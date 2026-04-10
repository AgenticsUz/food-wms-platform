import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./counterparties.component') }
];

export default routes;
