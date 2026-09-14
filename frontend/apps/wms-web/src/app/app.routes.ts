import type { Route } from '@angular/router';
import { authGuard, tenantGuard } from '@agentics/auth';

import { appShellGuard, moduleGuard, permissionGuard, portalGuard } from './core/auth/wms-guards';

/**
 * Ilova marshrutlari.
 *
 * Ikki zona:
 *  1. OCHIQ — kirish oqimi: `/login`, `/auth/callback`; tenant holati ekranlari
 *     (`/subscription` — obuna bloki, `/select-tenant` — tenant noma'lum) faqat
 *     `authGuard` ostida.
 *  2. HIMOYALANGAN — qobiq (`authGuard` + `tenantGuard`), ichida `/forbidden` va
 *     `/not-in-plan` ham bor: foydalanuvchi menyuni yo'qotmaydi.
 *
 * Bo'lim guard'lari eski `wms-ui/app.routes.ts` bilan BIR XIL (modul → ruxsat).
 * Bo'lim ichidagi marshrutlar `features/<bo'lim>/<bo'lim>.routes.ts` da — har
 * ekran ko'chiruvchi agent faqat O'Z faylini o'zgartiradi.
 *
 * O'CHGAN (PLATFORMA-TZ §7·F6.2): `auth/login|register|request-demo` (D5, D9 —
 * login Identity'da).
 *
 * QAYTDI (F9): `portal/*` — mijoz, ta'minotchi va agent kabineti. D8 uni «o'z
 * parol xeshi ikkinchi reyestr» sababi bilan olib tashlagan edi; endi hisob
 * Identity'da (`client`/`agent` roli), ya'ni sabab yo'q. Kabinet qobiqning
 * ICHIDA emas, YONIDA: uning menyusi, ruxsatlari va ekranlari boshqa.
 */
export const appRoutes: Route[] = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage),
  },
  {
    // Identity `redirect_uri` — Console'dagi `wms-web` mijozida aynan shu yo'l.
    path: 'auth/callback',
    loadComponent: () =>
      import('./pages/auth-callback/auth-callback.page').then((m) => m.AuthCallbackPage),
  },
  {
    path: 'subscription',
    canActivate: [authGuard],
    data: { kind: 'blocked' },
    loadComponent: () =>
      import('./pages/tenant-state/tenant-state.page').then((m) => m.TenantStatePage),
  },
  {
    path: 'select-tenant',
    canActivate: [authGuard],
    data: { kind: 'unknown' },
    loadComponent: () =>
      import('./pages/tenant-state/tenant-state.page').then((m) => m.TenantStatePage),
  },
  {
    path: 'portal',
    canActivate: [authGuard, tenantGuard, portalGuard()],
    loadChildren: () => import('./features/portal/portal.routes').then((m) => m.PORTAL_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard, tenantGuard, appShellGuard()],
    loadComponent: () =>
      import('./layout/shell/main-layout.component').then((m) => m.MainLayout),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'forbidden',
        loadComponent: () => import('./pages/forbidden/forbidden.page').then((m) => m.ForbiddenPage),
      },
      {
        path: 'not-in-plan',
        loadComponent: () =>
          import('./pages/not-in-plan/not-in-plan.page').then((m) => m.NotInPlanPage),
      },
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
      {
        path: 'warehouse',
        canActivate: [moduleGuard('WAREHOUSE_RAW'), permissionGuard('warehouse.view')],
        loadChildren: () =>
          import('./features/warehouse/warehouse.routes').then((m) => m.WAREHOUSE_ROUTES),
      },
      {
        path: 'production',
        canActivate: [moduleGuard('PRODUCTION'), permissionGuard('production.view')],
        loadChildren: () =>
          import('./features/production/production.routes').then((m) => m.PRODUCTION_ROUTES),
      },
      {
        path: 'transfers',
        canActivate: [moduleGuard('TRANSFERS'), permissionGuard('transfers.view')],
        loadChildren: () =>
          import('./features/transfers/transfers.routes').then((m) => m.TRANSFERS_ROUTES),
      },
      {
        path: 'finance',
        canActivate: [moduleGuard('FINANCE'), permissionGuard('finance.view')],
        loadChildren: () => import('./features/finance/finance.routes').then((m) => m.FINANCE_ROUTES),
      },
      {
        path: 'kpi',
        canActivate: [moduleGuard('KPI'), permissionGuard('kpi.view')],
        loadChildren: () => import('./features/kpi/kpi.routes').then((m) => m.KPI_ROUTES),
      },
      {
        path: 'counterparties',
        canActivate: [permissionGuard('partners.view')],
        loadChildren: () =>
          import('./features/counterparties/counterparties.routes').then(
            (m) => m.COUNTERPARTIES_ROUTES
          ),
      },
      {
        path: 'agents',
        canActivate: [moduleGuard('AGENTS'), permissionGuard('agents.view')],
        loadChildren: () => import('./features/agents/agents.routes').then((m) => m.AGENTS_ROUTES),
      },
      {
        path: 'products',
        canActivate: [permissionGuard('products.view')],
        loadChildren: () =>
          import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
      {
        path: 'delivery',
        canActivate: [moduleGuard('DELIVERY'), permissionGuard('delivery.view')],
        loadChildren: () =>
          import('./features/delivery/delivery.routes').then((m) => m.DELIVERY_ROUTES),
      },
      {
        path: 'settings',
        loadChildren: () =>
          import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES),
      },
      {
        path: 'custom',
        loadChildren: () => import('./features/custom/custom.routes').then((m) => m.CUSTOM_ROUTES),
      },
    ],
  },
  // Noma'lum manzil: kirgan bo'lsa bosh sahifa, aks holda `authGuard` login'ga olib chiqadi.
  { path: '**', redirectTo: '' },
];
