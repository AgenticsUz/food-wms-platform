import type { WmsModuleCode } from '../core/auth/wms-me.model';

/**
 * Chap menyu — MA'LUMOT (eski `sidebar.component.ts` dagi `allNavItems`).
 *
 * Ko'rinish qoidasi eski ilova bilan BIR XIL va tartibi muhim:
 *   modul (tarifda bormi) → feature (tenantda yoqilganmi) → ruxsat (kim ishlatadi).
 * Modul o'chiq bo'lsa butun bo'lim yo'qoladi — feature yoki ruxsat uni yenga olmaydi.
 *
 * ⚠️ Bu faqat UX. Marshrutning o'zi xuddi shu kodlar bilan guard qilingan
 * (`app.routes.ts`, `features/*.routes.ts`), haqiqiy chegara esa backendda.
 * Kod qo'shilsa yoki o'zgarsa — IKKALA joyda.
 */
export interface NavChild {
  readonly key: string;
  readonly route: string;
  readonly permissionCode?: string;
  readonly featureCode?: string;
}

export interface NavItem {
  readonly key: string;
  readonly icon: string;
  readonly route?: string;
  readonly moduleCode?: WmsModuleCode;
  readonly permissionCode?: string;
  readonly featureCode?: string;
  readonly children?: readonly NavChild[];
}

export const WMS_NAV: readonly NavItem[] = [
  { key: 'nav.dashboard', icon: 'pi pi-th-large', route: '/dashboard' },
  {
    key: 'nav.warehouse',
    icon: 'pi pi-box',
    moduleCode: 'WAREHOUSE_RAW',
    permissionCode: 'warehouse.view',
    children: [
      { key: 'warehouse.stockOverview', route: '/warehouse' },
      { key: 'warehouse.warehouses', route: '/warehouse/warehouses' },
      { key: 'warehouse.locations', route: '/warehouse/locations', featureCode: 'warehouse.locations' },
      { key: 'warehouse.batches', route: '/warehouse/batches', featureCode: 'warehouse.batches' },
      { key: 'warehouse.movements', route: '/warehouse/movements' },
    ],
  },
  {
    key: 'nav.production',
    icon: 'pi pi-cog',
    moduleCode: 'PRODUCTION',
    permissionCode: 'production.view',
    children: [
      { key: 'production.orders', route: '/production/orders', featureCode: 'production.orders' },
      { key: 'production.recipes', route: '/production/recipes', featureCode: 'production.recipes' },
      { key: 'production.stages', route: '/production/stages', featureCode: 'production.stages' },
    ],
  },
  {
    key: 'nav.transfers',
    icon: 'pi pi-arrow-right-arrow-left',
    moduleCode: 'TRANSFERS',
    permissionCode: 'transfers.view',
    route: '/transfers',
  },
  {
    key: 'nav.finance',
    icon: 'pi pi-wallet',
    moduleCode: 'FINANCE',
    permissionCode: 'finance.view',
    children: [
      { key: 'finance.overview', route: '/finance' },
      { key: 'finance.transactions', route: '/finance/transactions', featureCode: 'finance.transactions' },
      { key: 'finance.debts', route: '/finance/debts', featureCode: 'finance.debts' },
      // Eski menyuda feature'siz edi, marshrutda esa `finance.payments` bor edi —
      // menyu ochiq, sahifa yopiq holat. Endi ikkalasi bir xil.
      { key: 'finance.payments', route: '/finance/payments', featureCode: 'finance.payments' },
    ],
  },
  {
    key: 'nav.kpi',
    icon: 'pi pi-chart-line',
    moduleCode: 'KPI',
    permissionCode: 'kpi.view',
    children: [
      { key: 'kpi.dashboard', route: '/kpi' },
      { key: 'kpi.shifts', route: '/kpi/shifts', featureCode: 'kpi.shifts' },
      // Eski menyuda feature'siz, marshrutda `kpi.plans` — yuqoridagi bilan bir xil sabab.
      { key: 'kpi.plans', route: '/kpi/plans', featureCode: 'kpi.plans' },
      { key: 'kpi.actuals', route: '/kpi/actuals', featureCode: 'kpi.plans' },
      { key: 'kpi.attendance', route: '/kpi/attendance', featureCode: 'kpi.attendance' },
    ],
  },
  {
    key: 'nav.counterparties',
    icon: 'pi pi-users',
    permissionCode: 'partners.view',
    children: [
      { key: 'partners.suppliers', route: '/counterparties/suppliers', featureCode: 'counterparties.suppliers' },
      { key: 'partners.clients', route: '/counterparties/clients', featureCode: 'counterparties.clients' },
    ],
  },
  {
    key: 'nav.products',
    icon: 'pi pi-tags',
    permissionCode: 'products.view',
    children: [
      { key: 'products.products', route: '/products' },
      { key: 'products.categories', route: '/products/categories' },
      { key: 'products.units', route: '/products/units' },
    ],
  },
  {
    key: 'nav.agents',
    icon: 'pi pi-id-card',
    route: '/agents',
    moduleCode: 'AGENTS',
    permissionCode: 'agents.view',
  },
  // Custom fitcha namunasi — faqat feature yoqilgan tenantda ko'rinadi (docs/CUSTOM_FEATURES.md).
  {
    key: 'custom.exampleFeature.title',
    icon: 'pi pi-star',
    route: '/custom/example-feature',
    featureCode: 'custom.example-feature',
  },
  {
    key: 'nav.delivery',
    icon: 'pi pi-truck',
    moduleCode: 'DELIVERY',
    permissionCode: 'delivery.view',
    children: [
      { key: 'delivery.deliveries', route: '/delivery' },
      { key: 'delivery.vehicles', route: '/delivery/vehicles' },
      { key: 'delivery.drivers', route: '/delivery/drivers' },
    ],
  },
];

export const SETTINGS_NAV: NavItem = {
  key: 'nav.settings',
  icon: 'pi pi-sliders-h',
  children: [
    { key: 'settings.users', route: '/settings/users', permissionCode: 'settings.users' },
    { key: 'settings.roles', route: '/settings/roles', permissionCode: 'settings.roles' },
    { key: 'settings.modules', route: '/settings/modules', permissionCode: 'settings.modules' },
    { key: 'subscription.title', route: '/settings/subscription' },
    // Marshrutda `quality.view` ham talab qilinadi — menyu ham shuni tekshiradi.
    {
      key: 'settings.qcParameters',
      route: '/settings/qc-parameters',
      featureCode: 'qc.parameters',
      permissionCode: 'quality.view',
    },
    { key: 'settings.audit', route: '/settings/audit', permissionCode: 'audit.view' },
    { key: 'settings.profile', route: '/settings/profile' },
  ],
};
