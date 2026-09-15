import type { Routes } from '@angular/router';

import { featureGuard, permissionGuard, roleGuard } from '../../core/auth/wms-guards';

/**
 * `/settings` — ota marshrut guard'siz: profil har kimga ochiq. Ruxsat kodlari
 * backend `[RequirePermission]` bilan bir xil. `data.titleKey` — `i18n.spec`
 * sarlavha kalitini tekshiradi.
 *
 * D5/D7: Foydalanuvchilar ekrani — ro'yxat va WMS rollarini tahrirlash (yaratish,
 * parol tiklash Console/Identity'da); Profil — parol almashtirishsiz.
 *
 * F8.1: «Kirish hisoblari» (`access`) — tashkilotning Identity login hisoblari
 * (qo'shish, rol, parol tiklash). Sahifa TAYYOR holda `@agentics/identity-tenant`
 * paketidan keladi va Identity `/tenant/v1` ga to'g'ridan-to'g'ri boradi (WMS
 * API'si emas). Guard — ruxsat kodi emas, Identity `admin` roli: API ham
 * aynan shuni tekshiradi.
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
    path: 'access',
    canActivate: [roleGuard('admin')],
    data: { titleKey: 'settings.accessAccounts' },
    loadComponent: () => import('@agentics/identity-tenant').then((m) => m.AccessAccountsPage),
  },
  {
    path: 'modules',
    canActivate: [permissionGuard('settings.modules')],
    data: { titleKey: 'settings.modules' },
    loadComponent: () => import('./modules/modules.component'),
  },
  /**
   * P2.6: standart ombor. Guard — `warehouse.view` (API'da GET shu ruxsat ostida);
   * tenant sozlamasini YOZISH uchun `settings.modules` kerak, uni sahifaning o'zi
   * tekshiradi — ruxsatsiz odam ham «hozir qaysi ombor» ni ko'ra olsin.
   */
  {
    path: 'warehouses',
    canActivate: [permissionGuard('warehouse.view')],
    data: { titleKey: 'settings.warehouseDefaults' },
    loadComponent: () => import('./warehouse-defaults/warehouse-defaults.component'),
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
