import type { Routes } from '@angular/router';

/**
 * Bosh sahifa — guard'siz: har kirgan foydalanuvchi ko'radi (kartalar va
 * so'rovlar modul/feature/ruxsat bo'yicha komponentning o'zida yashiriladi).
 *
 * `data.titleKey` qoldirildi — ekran nomi kaliti, `i18n.spec` uni tekshiradi.
 */
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'nav.dashboard' },
    loadComponent: () => import('./dashboard.component'),
  },
];
