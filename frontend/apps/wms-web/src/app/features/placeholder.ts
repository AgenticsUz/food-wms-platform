import type { Route } from '@angular/router';

/**
 * Hali ko'chirilmagan ekran uchun marshrut: guard'lar TAYYOR, komponent —
 * «ko'chirilmoqda» sahifasi.
 *
 * Ekranni ko'chirgan agent SHU QATORNI almashtiradi, guard'larni saqlab:
 *
 * ```ts
 * // oldin
 * placeholder('batches', 'warehouse.batches', 'warehouse/batches', { canActivate: [featureGuard('warehouse.batches')] }),
 * // keyin
 * { path: 'batches', canActivate: [featureGuard('warehouse.batches')],
 *   loadComponent: () => import('./batches/batches.component') },
 * ```
 *
 * @param titleKey ekran nomi kaliti (menyudagi bilan bir xil) — sahifa sarlavhasi.
 * @param source eski `wms-ui/src/app/modules/` ostidagi komponent papkasi.
 */
export function placeholder(
  path: string,
  titleKey: string,
  source: string,
  extra: Omit<Route, 'path' | 'loadComponent' | 'data'> = {}
): Route {
  return {
    ...extra,
    path,
    data: { titleKey, source: `wms-ui/src/app/modules/${source}` },
    loadComponent: () =>
      import('../shared/components/migration-placeholder/migration-placeholder.page').then(
        (m) => m.MigrationPlaceholderPage
      ),
  };
}
