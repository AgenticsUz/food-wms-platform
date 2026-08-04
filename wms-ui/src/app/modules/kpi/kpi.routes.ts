import { Routes } from '@angular/router';
import { featureGuard } from '../../core/guards/feature.guard';

const routes: Routes = [
  { path: '', loadComponent: () => import('./dashboard/kpi-dashboard.component') },
  { path: 'shifts', canActivate: [featureGuard('kpi.shifts')], loadComponent: () => import('./shifts/shifts.component') },
  { path: 'plans', loadComponent: () => import('./plans/plans.component') },
  { path: 'actuals', loadComponent: () => import('./actuals/actuals.component') },
  { path: 'attendance', canActivate: [featureGuard('kpi.attendance')], loadComponent: () => import('./attendance/attendance.component') }
];

export default routes;
