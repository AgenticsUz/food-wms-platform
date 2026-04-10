import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', redirectTo: 'users', pathMatch: 'full' },
  { path: 'users', loadComponent: () => import('./users/users.component') },
  { path: 'roles', loadComponent: () => import('./roles/roles.component') },
  { path: 'modules', loadComponent: () => import('./modules/modules.component') },
  { path: 'qc-parameters', loadComponent: () => import('./qc-parameters/qc-parameters.component') },
  { path: 'profile', loadComponent: () => import('./profile/profile.component') }
];

export default routes;
