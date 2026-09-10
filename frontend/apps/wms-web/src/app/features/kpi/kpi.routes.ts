import type { Routes } from '@angular/router';

import { featureGuard } from '../../core/auth/wms-guards';

/**
 * `/kpi` — ota marshrutda `moduleGuard('KPI')` + `permissionGuard('kpi.view')`.
 *
 * `data.titleKey` ko'chirilgach ham qoldirildi: `i18n.spec` sarlavha kalitlarini
 * tarjimada borligini shu orqali tekshiradi.
 */
export const KPI_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'kpi.dashboard' },
    loadComponent: () => import('./dashboard/kpi-dashboard.component'),
  },
  {
    path: 'shifts',
    canActivate: [featureGuard('kpi.shifts')],
    data: { titleKey: 'kpi.shifts' },
    loadComponent: () => import('./shifts/shifts.component'),
  },
  {
    path: 'plans',
    canActivate: [featureGuard('kpi.plans')],
    data: { titleKey: 'kpi.plans' },
    loadComponent: () => import('./plans/plans.component'),
  },
  // Eski ilovadagidek: haqiqiy ko'rsatkichlar rejalar feature'i ostida (backend ham `kpi.plans`).
  {
    path: 'actuals',
    canActivate: [featureGuard('kpi.plans')],
    data: { titleKey: 'kpi.actuals' },
    loadComponent: () => import('./actuals/actuals.component'),
  },
  {
    path: 'attendance',
    canActivate: [featureGuard('kpi.attendance')],
    data: { titleKey: 'kpi.attendance' },
    loadComponent: () => import('./attendance/attendance.component'),
  },
];
