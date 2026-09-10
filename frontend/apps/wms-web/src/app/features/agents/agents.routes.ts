import type { Routes } from '@angular/router';

/**
 * `/agents` — ota marshrutda `moduleGuard('AGENTS')` + `permissionGuard('agents.view')`. Agent PORTALI yo'q (D8).
 * `data.titleKey` qoldirildi — `i18n.spec.ts` sarlavha kalitlarini tekshiradi.
 */
export const AGENTS_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'nav.agents' },
    loadComponent: () => import('./agent-list/agent-list.component'),
  },
  {
    path: ':id',
    data: { titleKey: 'nav.agents' },
    loadComponent: () => import('./agent-detail/agent-detail.component'),
  },
];
