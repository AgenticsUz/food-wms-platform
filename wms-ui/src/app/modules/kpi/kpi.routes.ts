import { Routes } from '@angular/router';

const routes: Routes = [
  { path: '', loadComponent: () => import('./dashboard/kpi-dashboard.component') },
  { path: 'shifts', loadComponent: () => import('./shifts/shifts.component') },
  { path: 'plans', loadComponent: () => import('./plans/plans.component') },
  { path: 'actuals', loadComponent: () => import('./actuals/actuals.component') },
  { path: 'attendance', loadComponent: () => import('./attendance/attendance.component') }
];

export default routes;
