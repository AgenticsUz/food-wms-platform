import type { Routes } from '@angular/router';

import { featureGuard, permissionGuard } from '../../core/auth/wms-guards';

/**
 * `/settings` — ota marshrut guard'siz: profil har kimga ochiq. Ruxsat kodlari
 * backend `[RequirePermission]` bilan bir xil. `data.titleKey` — `i18n.spec`
 * sarlavha kalitini tekshiradi.
 *
 * D5/D7: Foydalanuvchilar ekrani — ro'yxat va WMS rollarini tahrirlash (yaratish,
 * parol tiklash Console/Identity'da); Profil — parol almashtirishsiz.
 */
export const SETTINGS_ROUTES: Routes = [
  { path: '', redirectTo: 'profile', pathMatch: 'full' },
  {
    path: 'users',
    canActivate: [permissionGuard('settings.users')],
    data: { titleKey: 'settings.users' },
    loadComponent: () => import('./users/users.component'),
  },
  {
    path: 'roles',
    canActivate: [permissionGuard('settings.roles')],
    data: { titleKey: 'settings.roles' },
    loadComponent: () => import('./roles/roles.component'),
  },
  {
    path: 'modules',
    canActivate: [permissionGuard('settings.modules')],
    data: { titleKey: 'settings.modules' },
    loadComponent: () => import('./modules/modules.component'),
  },
  {
    path: 'subscription',
    data: { titleKey: 'subscription.title' },
    loadComponent: () => import('./subscription/subscription.component'),
  },
  {
    path: 'qc-parameters',
    canActivate: [featureGuard('qc.parameters'), permissionGuard('quality.view')],
    data: { titleKey: 'settings.qcParameters' },
    loadComponent: () => import('./qc-parameters/qc-parameters.component'),
  },
  {
    path: 'audit',
    canActivate: [permissionGuard('audit.view')],
    data: { titleKey: 'settings.audit' },
    loadComponent: () => import('./audit-log/audit-log.component'),
  },
  {
    path: 'profile',
    data: { titleKey: 'settings.profile' },
    loadComponent: () => import('./profile/profile.component'),
  },
];
