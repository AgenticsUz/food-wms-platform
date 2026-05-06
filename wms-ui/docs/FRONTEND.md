# WMS Frontend — Task Document

> WebStorm Claude Code agent reference.
> Complete tasks in order. After each phase: build → browser check → next phase.

---

## PROJECT INFO

Framework: Angular 21 (standalone, signals, zoneless)
UI: PrimeNG 21.1.5
CSS: Tailwind CSS v4 + SCSS
Charts: ApexCharts + ng-apexcharts
i18n: Transloco (uz, ru, en)
Backend: http://localhost:7040/api
Port: 7050

### Coding rules
- Use inject() — never constructor injection
- ChangeDetectionStrategy.OnPush on all components
- signal(), computed(), effect() for state
- ApiService wrapper — never inject HttpClient directly
- NotificationService for all success/error messages
- All text through transloco pipe

---

## BACKEND REFERENCE

### New endpoints added
PUT    /api/categories/{id}
DELETE /api/categories/{id}
PUT    /api/units/{id}
DELETE /api/units/{id}
PUT    /api/shifts/{id}
DELETE /api/shifts/{id}
PUT    /api/roles/{id}
DELETE /api/warehouses/{id}
GET    /api/transfers — pagination fixed (default pageSize=50)

### Permission endpoints (new)
GET  /api/permissions
GET  /api/roles/{id}/permissions
PUT  /api/roles/{id}/permissions  body: { permissionIds: int[] }
GET  /api/auth/my-permissions

### GET /api/auth/me now returns permissions
{ "id":1, "fullName":"Admin", "roles":["Admin"], "permissions":["dashboard.view",...] }

### Permission codes
dashboard.view
warehouse.view, warehouse.manage
transfers.view, transfers.create, transfers.confirm, transfers.reject
production.view, production.manage
finance.view, finance.manage
kpi.view, kpi.manage
partners.view, partners.manage
products.view, products.manage
settings.users, settings.roles, settings.modules
quality.view, quality.manage

---

## PHASE 1 — BUG FIXES

### 1.1 Transfers list shows only 1 record
Check TransferService — pageSize is likely 1.
Fix: page=1, pageSize=50. All transfers must be visible.

### 1.2 i18n not working
Problem: only sidebar translates, all other pages have hardcoded English.

Fix:
1. Replace all hardcoded strings with {{ 'key' | transloco }} pipe
2. Import TranslocoModule in every component
3. Fill uz.json, ru.json, en.json with these keys:

common: save, cancel, delete, edit, create, search, confirm, reject, back,
        actions, status, note, date, yes, no, add, update, close, loading,
        noData, total, name, description, type, code, price, quantity,
        unit, amount, from, to, refresh, active, inactive, all

auth: login, logout, phone, password, organizationCode, signIn, welcome

nav: dashboard, warehouse, production, transfers, finance, kpi, partners,
     products, settings, stockOverview, warehouses, locations, batches,
     movements, stages, recipes, orders, suppliers, clients, categories,
     units, users, roles, modules, profile, attendance, shifts, plans,
     actuals, transactions, debts, payments, overview

status: pending, confirmed, rejected, cancelled, inStock, lowStock,
        expired, expiringSoon, draft, inProgress, completed, active,
        inactive, ok, all

warehouse: title, stockOverview, currentInventory, searchProduct,
           allWarehouses, lotNumber, expiry, reserved, available,
           noStockData, addWarehouse, addLocation

transfer: title, newTransfer, selectType, incoming, outgoing, internal,
          selectSupplier, selectClient, selectWarehouse, addItem,
          totalAmount, confirmTransfer, rejectTransfer, items

production: title, stages, recipes, orders, newOrder, plannedQty,
            actualQty, wasteQty, reworkQty, startOrder, completeOrder,
            executeStage

finance: title, income, expense, debt, payment, totalIncome, totalExpense,
         netAmount, totalDebt, recordPayment, paymentMethod, cash, bank, card

kpi: title, efficiency, planned, actual, waste, checkIn, checkOut,
     attendance, shift

settings: title, users, roles, modules, profile, changePassword,
          newPassword, currentPassword, assignRoles, permissions,
          enableModule, disableModule

dashboard: goodMorning, goodAfternoon, goodEvening, totalStock, efficiency,
           pendingTransfers, revenue, lowStockItems, recentTransfers,
           noData, lastUpdated

Test: switch to RU — ALL text on ALL pages must change.

### 1.3 Fix CRUD with new endpoints

Categories: Edit → PUT /api/categories/{id}, Delete → DELETE /api/categories/{id}
Units: Edit → PUT /api/units/{id}, Delete → DELETE /api/units/{id}
Shifts: Edit → PUT /api/shifts/{id}, Delete → DELETE /api/shifts/{id}
Roles: Edit → PUT /api/roles/{id}
Warehouses: Delete → DELETE /api/warehouses/{id}

All deletions must use NotificationService.confirmDelete() first.

---

## PHASE 2 — TAILWIND + UI

### 2.1 Verify Tailwind setup
tailwind.config.ts: content: ['./src/**/*.{html,ts}']
styles.scss: @import tailwindcss/base, components, utilities

### 2.2 Layout
Main content area transitions smoothly when sidebar collapses:
- Expanded: ml-[260px]
- Collapsed: ml-[72px]
- transition-all duration-300

### 2.3 Shared components with Tailwind

PageHeaderComponent:
- flex items-center justify-between mb-6
- Title: text-2xl font-bold text-gray-900 dark:text-white
- Subtitle: text-sm text-gray-500 mt-0.5
- Actions slot: flex items-center gap-3

StatusBadgeComponent — Tailwind per status:
- confirmed/success: bg-emerald-100 text-emerald-800 px-2.5 py-0.5 rounded-full text-xs font-semibold
- pending/warning:   bg-amber-100 text-amber-800
- rejected/error:    bg-red-100 text-red-800
- draft/neutral:     bg-gray-100 text-gray-600
- inProgress:        bg-blue-100 text-blue-800

EmptyStateComponent:
- flex flex-col items-center justify-center py-16 text-gray-400
- Icon: pi text-5xl mb-4
- Message: text-base font-medium

### 2.4 Cards

All cards:
  bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-100
  dark:border-gray-700 p-5 transition-all hover:-translate-y-0.5 hover:shadow-md

Dashboard summary cards (5 different icon colors):
  Total Stock:       icon bg-indigo-100, icon text-indigo-600
  Efficiency:        icon bg-emerald-100, icon text-emerald-600
  Pending Transfers: icon bg-amber-100, icon text-amber-600
  Revenue:           icon bg-blue-100, icon text-blue-600
  Low Stock Alert:   icon bg-red-100, icon text-red-600

Each card: icon w-12 h-12 rounded-xl, number text-3xl font-bold font-mono,
           trend ↑ text-emerald-600 / ↓ text-red-500 text-xs font-semibold

### 2.5 Tables

Wrap all tables:
  bg-white dark:bg-gray-800 rounded-2xl shadow-sm
  border border-gray-100 dark:border-gray-700 overflow-hidden

Header cells: bg-gray-50, text-xs uppercase tracking-wide font-700 text-gray-500,
              sticky top-0, border-bottom
Rows: transition 0.15s, hover indigo-tint, even rows very light bg

### 2.6 Buttons

Primary:   bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg
           text-sm font-semibold transition-all active:scale-95
Outlined:  border border-indigo-600 text-indigo-600 hover:bg-indigo-50
Danger:    bg-red-500 hover:bg-red-600 text-white

### 2.7 Forms

Labels: text-xs font-semibold uppercase tracking-wide text-gray-500 mb-1.5
Inputs: focus:ring-2 focus:ring-indigo-500 focus:border-transparent

### 2.8 Dashboard layout

Summary cards: grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-5 mb-6
Full width chart: bg-white rounded-2xl p-5 border mb-5
Half charts: grid grid-cols-1 lg:grid-cols-2 gap-5 mb-5
Recent table: bg-white rounded-2xl border overflow-hidden

### 2.9 Breadcrumb

Add below page title: Dashboard > Warehouse > Stock Overview
Text-xs, muted color, separator /, last item slightly darker

### 2.10 Mobile (< 768px)

Sidebar: hidden, hamburger button in header (md:hidden)
Tables: overflow-x-auto
Grids: grid-cols-1
Dialogs: full screen

### 2.11 Page transition

styles.scss:
  @keyframes fadeSlideIn { from: opacity 0, translateY(8px); to: opacity 1, translateY(0) }
  .page-enter { animation: fadeSlideIn 0.2s ease-out }

Add class="page-enter" to top div of every page component.

Zero errors. Check light + dark mode on every page.

---

## PHASE 3 — ANALYTICS

### 3.1 Global ApexCharts config

Create core/config/apex-defaults.ts:
  chart: { fontFamily: DM Sans, toolbar: hidden, animations: easeinout 600ms }
  colors: ['#6366f1', '#10b981', '#f59e0b', '#3b82f6', '#ef4444', '#8b5cf6']
  grid: borderColor #e2e8f0, strokeDashArray 4
  tooltip: theme light, DM Sans font

Spread APEX_DEFAULTS in every chart component.
When ThemeService.isDark() changes — update all charts theme.

### 3.2 Dashboard improvements

Date range filter (top right of dashboard): 7d / 30d / 90d buttons
Selecting a range refreshes ALL charts simultaneously.

Chart 1 — Production Plan vs Actual
  Type: line, height: 280, full width
  Series: Planned + Actual
  stroke: smooth, markers: size 4
  API: GET /api/analytics/production/plan-vs-actual?days={days}

Chart 2 — Daily Transfers
  Type: bar, height: 240, half width
  Series: Incoming + Outgoing
  plotOptions: borderRadius 4, columnWidth 60%
  API: GET /api/analytics/transfers/daily?days={days}

Chart 3 — Product Distribution
  Type: donut, height: 240, half width
  donut size: 65%, legend: bottom
  API: GET /api/analytics/products/distribution

Recent transfers table: last 5, clickable rows navigate to /transfers/{id}

### 3.3 Module charts

Warehouse: stock levels bar chart
  API: GET /api/analytics/warehouse/stock-levels
  Red bar when stock <= minStock

Finance overview: area chart with gradient fill (income vs expense)
  API: GET /api/analytics/finance/income-expense?days=30
  Also: top debtors bar chart
  API: GET /api/analytics/finance/top-debtors?top=5

KPI: radialBar chart for efficiency %
  API: GET /api/analytics/kpi/shift-efficiency?days=7
  hollow: 60%, large centered percentage label

Production: waste by stage bar chart
  API: GET /api/analytics/production/waste-by-stage?days=30

All charts: loading skeleton while fetching + empty state when no data.

---

## PHASE 4 — PERMISSION SYSTEM

### 4.1 PermissionService

Create core/services/permission.service.ts:

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private permissions = signal<string[]>([]);
  setPermissions(codes: string[]) { this.permissions.set(codes); }
  can(code: string): boolean { return this.permissions().includes(code); }
  canAny(...codes: string[]): boolean { return codes.some(c => this.can(c)); }
}

### 4.2 Load permissions after login

In AuthService.login() after token is set:
- Call GET /api/auth/my-permissions (or read from auth/me response)
- permissionService.setPermissions(permissions)
- localStorage.setItem('permissions', JSON.stringify(permissions))

On page refresh: read from localStorage and restore.

### 4.3 HasPermissionDirective

Create shared/directives/has-permission.directive.ts
Structural directive: *hasPermission="'code'"
If user does NOT have the permission — element is removed from DOM (not hidden).
Uses ViewContainerRef + TemplateRef pattern.

### 4.4 Dynamic sidebar

navItems = computed(() => [...])
Each item: { label, icon, route, visible: perm.can('code'), children?: [...] }
Items with visible=false are not rendered at all.

### 4.5 Apply to action buttons

Transfers:
  *hasPermission="'transfers.create'"  → New Transfer button
  *hasPermission="'transfers.confirm'" → Confirm button
  *hasPermission="'transfers.reject'"  → Reject button

Products:
  *hasPermission="'products.manage'"   → Add / Edit / Delete buttons

Finance:
  *hasPermission="'finance.manage'"    → Add Transaction, Record Payment

Partners:
  *hasPermission="'partners.manage'"   → Add Supplier, Add Client

Production:
  *hasPermission="'production.manage'" → New Order, Execute Stage

Settings tabs:
  *hasPermission="'settings.users'"    → Users tab
  *hasPermission="'settings.roles'"    → Roles tab
  *hasPermission="'settings.modules'"  → Modules tab

### 4.6 Role permissions management

In Settings → Roles → Edit dialog add Permissions tab:
  Permissions grouped by module with checkboxes
  Load: GET /api/roles/{id}/permissions
  Save: PUT /api/roles/{id}/permissions { permissionIds: [...] }
  
  Layout:
    WAREHOUSE    [x] View  [x] Manage
    TRANSFERS    [x] View  [x] Create  [x] Confirm  [x] Reject
    PRODUCTION   [x] View  [ ] Manage
    FINANCE      [x] View  [ ] Manage
    ...

### 4.7 Route guards

export const permissionGuard = (code: string): CanActivateFn => () => {
  const perm = inject(PermissionService);
  if (perm.can(code)) return true;
  inject(Router).navigate(['/dashboard']);
  return false;
};

Apply to routes: finance, kpi, settings.

Zero errors.
Test: create role with only transfers.view → login → only Transfers visible.

---

## GENERAL NOTES

- ng build → 0 errors after every phase
- Dark mode works on all new components
- All text through transloco pipe
- Skeleton loaders while data loads
- Empty state when lists are empty
- NotificationService for all toasts
- hasPermission directive on all action buttons
