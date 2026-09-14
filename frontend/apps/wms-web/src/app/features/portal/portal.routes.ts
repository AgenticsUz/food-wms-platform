import type { Routes } from '@angular/router';

/**
 * Kabinet marshrutlari (F9) — ilova qobig'idan ALOHIDA daraxt.
 *
 * Guard'lar ota marshrutda (`app.routes.ts`: `authGuard`, `tenantGuard`,
 * `portalGuard`). Bu yerda ruxsat kodi YO'Q: kabinet rollarining WMS ruxsati
 * ataylab bo'sh, chegarani server qo'yadi (`PortalService` aktyorni tokendan
 * yechadi va topolmasa 403 beradi).
 *
 * «Mijozlarim» faqat agentga ma'noli, lekin marshrut guard'i qo'yilmagan:
 * kontragent u yerga tushsa API 403 beradi va sahifa bo'sh holatni ko'rsatadi —
 * menyuda esa u bandni ko'rmaydi.
 */
export const PORTAL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/portal-layout.component').then((m) => m.PortalLayoutComponent),
    children: [
      {
        path: '',
        data: { titleKey: 'portal.overview' },
        loadComponent: () => import('./overview/portal-overview.component'),
      },
      {
        path: 'transfers',
        data: { titleKey: 'portal.transfers' },
        loadComponent: () => import('./transfers/portal-transfers.component'),
      },
      {
        path: 'payments',
        data: { titleKey: 'portal.payments' },
        loadComponent: () => import('./payments/portal-payments.component'),
      },
      {
        path: 'clients',
        data: { titleKey: 'portal.clients' },
        loadComponent: () => import('./clients/portal-clients.component'),
      },
      { path: '**', redirectTo: '' },
    ],
  },
];
