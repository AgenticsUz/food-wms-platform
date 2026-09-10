import type { Routes } from '@angular/router';

/**
 * `/products` — ota marshrutda `permissionGuard('products.view')` (modulsiz).
 *
 * `data.titleKey` ko'chirilgandan keyin ham qoldirildi: ekran nomi kaliti
 * (menyudagi bilan bir xil) va `i18n.spec` uni tarjimada borligini tekshiradi.
 */
export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    data: { titleKey: 'products.products' },
    loadComponent: () => import('./product-list/product-list.component'),
  },
  {
    path: 'categories',
    data: { titleKey: 'products.categories' },
    loadComponent: () => import('./categories/categories.component'),
  },
  {
    path: 'units',
    data: { titleKey: 'products.units' },
    loadComponent: () => import('./units/units.component'),
  },
];
