import type { Route } from '@angular/router';

import { AGENTS_ROUTES } from '../../features/agents/agents.routes';
import { COUNTERPARTIES_ROUTES } from '../../features/counterparties/counterparties.routes';
import { CUSTOM_ROUTES } from '../../features/custom/custom.routes';
import { DASHBOARD_ROUTES } from '../../features/dashboard/dashboard.routes';
import { DELIVERY_ROUTES } from '../../features/delivery/delivery.routes';
import { FINANCE_ROUTES } from '../../features/finance/finance.routes';
import { KPI_ROUTES } from '../../features/kpi/kpi.routes';
import { PRODUCTION_ROUTES } from '../../features/production/production.routes';
import { PRODUCTS_ROUTES } from '../../features/products/products.routes';
import { SETTINGS_ROUTES } from '../../features/settings/settings.routes';
import { TRANSFERS_ROUTES } from '../../features/transfers/transfers.routes';
import { WAREHOUSE_ROUTES } from '../../features/warehouse/warehouse.routes';
import { SETTINGS_NAV, WMS_NAV } from '../../layout/nav';
import { WMS_MODULES } from '../auth/wms-me.model';
import ru from './ru.json';
import uzLatn from './uz-Latn.json';

type Tree = Readonly<Record<string, unknown>>;

function flatten(tree: Tree, prefix = ''): string[] {
  return Object.entries(tree).flatMap(([key, value]) =>
    value !== null && typeof value === 'object' && !Array.isArray(value)
      ? flatten(value as Tree, `${prefix}${key}.`)
      : [`${prefix}${key}`]
  );
}

const uzKeys = new Set(flatten(uzLatn));

/**
 * Drift qorovuli: `uz-Latn` (manba) va `ru` bir xil kalitlar to'plamiga ega.
 * Kalit bitta faylga qo'shilib ikkinchisiga unutilsa ruscha foydalanuvchi xom
 * kalitni ko'radi (`uz-Cyrl` alohida fayl emas — transliteratsiya).
 */
describe('WMS ildiz tarjimalari', () => {
  it('uz-Latn va ru kalitlari bir xil', () => {
    const ruKeys = new Set(flatten(ru));
    expect([...uzKeys].filter((k) => !ruKeys.has(k))).toEqual([]);
    expect([...ruKeys].filter((k) => !uzKeys.has(k))).toEqual([]);
  });

  it('menyudagi har kalit tarjimada bor', () => {
    const keys = [...WMS_NAV, SETTINGS_NAV].flatMap((item) => [
      item.key,
      ...(item.children ?? []).map((c) => c.key),
    ]);
    expect(keys.filter((k) => !uzKeys.has(k))).toEqual([]);
  });

  it('har «ko\'chirilmoqda» marshrutining sarlavha kaliti tarjimada bor', () => {
    const all: readonly Route[] = [
      ...DASHBOARD_ROUTES,
      ...WAREHOUSE_ROUTES,
      ...PRODUCTION_ROUTES,
      ...TRANSFERS_ROUTES,
      ...FINANCE_ROUTES,
      ...KPI_ROUTES,
      ...COUNTERPARTIES_ROUTES,
      ...AGENTS_ROUTES,
      ...PRODUCTS_ROUTES,
      ...DELIVERY_ROUTES,
      ...SETTINGS_ROUTES,
      ...CUSTOM_ROUTES,
    ];
    const titleKeys = all
      .map((r) => r.data?.['titleKey'])
      .filter((k): k is string => typeof k === 'string');
    expect(titleKeys.length).toBeGreaterThan(0);
    expect(titleKeys.filter((k) => !uzKeys.has(k))).toEqual([]);
  });

  it('har modul kodining nomi bor («tarifingizda yo\'q» ekrani)', () => {
    expect(WMS_MODULES.filter((code) => !uzKeys.has(`shell.modules.${code}`))).toEqual([]);
  });
});
