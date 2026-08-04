import { Routes } from '@angular/router';

const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component') },
  { path: 'register', loadComponent: () => import('./register/register.component') },
  // Yangi nom — eski havolalar ishlashda davom etadi
  { path: 'request-demo', redirectTo: 'register' },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];

export default routes;
