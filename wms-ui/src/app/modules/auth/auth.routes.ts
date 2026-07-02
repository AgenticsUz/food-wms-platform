import { Routes } from '@angular/router';

const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component') },
  { path: 'register', loadComponent: () => import('./register/register.component') },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];

export default routes;
