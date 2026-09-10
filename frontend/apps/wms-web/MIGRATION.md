# wms-web — ekranlarni ko'chirish xaritasi

`wms-ui` (eski, Nx'siz) → `frontend/apps/wms-web`. Qobiq, marshrut daraxti, guard'lar,
HTTP qatlami, i18n va umumiy komponentlar TAYYOR. Har ekran hozir «ko'chirilmoqda»
sahifasi (`placeholder(...)`). Ko'chiruvchi agent FAQAT o'z bo'limining
`src/app/features/<bo'lim>/` papkasiga tegadi.

## Qanday ko'chiriladi

1. Komponentni `wms-ui/src/app/modules/<x>/…` dan `src/app/features/<bo'lim>/<x>/` ga ko'chiring
   (`export default class` qolishi mumkin).
2. `features/<bo'lim>/<bo'lim>.routes.ts` da `placeholder(path, …, { canActivate })` qatorini
   `{ path, canActivate, loadComponent: () => import('./x/x.component') }` ga almashtiring —
   **guard'lar saqlanadi**.
3. Domen servisi va modeli — `features/<bo'lim>/` ichida (`*.service.ts`, `*.model.ts`).
   Bir nechta bo'lim ishlatadigani (masalan `product.model`) — o'z bo'limida, boshqalar import qiladi.
4. `npx nx run-many -t lint,test,build -p wms-web` — yashil.

## Konvensiyalar (qobiq qo'ygan)

| Eski | Yangi | Izoh |
|---|---|---|
| `core/services/api.service` | `core/api/api.service` → `ApiService` | Imzo BIR XIL: `get<T>(path, params?)` → `Observable<ApiResponse<T>>`. Yo'l `/api` siz (`'warehouses'`). Uchinchi argument `{ skipErrorNotify, skipLoading }`. `upload()`, `download()` qo'shildi. `Date` parametr → UTC ISO |
| `core/models/api-response.model` | `core/api/api-response.model` | Shakl o'zgarmagan (D15) |
| `err.error?.message` | o'zgarishsiz ishlaydi | Xato endi `WmsApiError`: `message`, `wmsCode`, `status`, `kind`, `error` (asl konvert) |
| `error.interceptor` toasti | `WmsErrorNotifier` | Toast allaqachon chiqadi — komponent ikkinchisini qo'shmaydi |
| `shared/services/notification.service` | `core/notify/notification.service` | API bir xil (`success/error/warn/info/confirmDelete/confirmAction`) |
| `PermissionService.can()` | `WmsSession.can()` / `canAny()` | `core/auth/wms-session` |
| `TenantService.isModuleEnabled()` | `WmsSession.isModuleEnabled()` | |
| `FeatureService.isEnabled()` | `WmsSession.isFeatureEnabled()` | ⚠️ bo'sh ro'yxat = hammasi YOPIQ (fail-closed) |
| `SubscriptionService.info()` | `WmsSession.subscription()` (qisqa) | To'liq obuna/limitlar — `settings/subscription` ekrani o'zi `subscription/me` ni o'qiydi; `ApiService.lastWarning` signaliga qarab yangilaydi |
| `AuthService.currentUser()` | `WmsSession.me()` / `fullName()` / `roles()` | Id'lar Guid satr |
| `permissionGuard/moduleGuard/featureGuard` | `core/auth/wms-guards` | Chaqiruv bir xil; rad etilsa `/forbidden` yoki `/not-in-plan` |
| `*hasPermission` | `shared/directives/has-permission.directive` | Selektor o'sha |
| `shared/utils/date.util` | `core/utils/date.util` | + `toUtcIso`, `localDayRangeToUtc` |
| `shared/utils/clipboard.util` | `core/utils/clipboard.util` | |
| `core/config/apex-defaults` | `core/config/apex-defaults` | ApexCharts 6 / ng-apexcharts 3 (wash versiyasi) |
| `export.service`, `import.service`, `currency.service`, `notification-bell.service` | `core/services/*` | Id'lar `string` |
| `page-header`, `empty-state`, `status-badge`, `phone-input`, `import-button`, `notification-bell`, `subscription-banner` | `shared/components/*` | |
| `ThemeService`, `BrandingService` | `core/theme`, `core/branding` | Brend `/api/me` dan avtomatik |
| `transloco.setActiveLang('uz')` | `LanguageService.setLanguage('uz-Latn')` (`@agentics/i18n`) | Tillar: `uz-Latn`, `uz-Cyrl`, `ru` |

**i18n.** Barcha eski kalitlar `core/i18n/uz-Latn.json` va `ru.json` da, nomlari o'zgarmagan.
Yangi kalit IKKALA faylga qo'shiladi (`i18n.spec.ts` tekshiradi); `uz-Cyrl` fayli YO'Q.
Istisnolar: `t('common.status')` → **`t('common.status.label')`** (platforma ildizida
`common.status` obyekt; eskisida 23 joyda ishlatilgan); `auth.login` (satr) o'chdi;
`agentPortal.*`, `superadmin.*`, `upgrade.*` o'chdi.

**Lint.** PrimeNG hamma joyda mumkin, komponentda `subscribe()` mumkin. Taqiq: `any`
(shablonda `$any()` ham — eskisida 74 ta), non-null `!`, `HttpClient` komponentda,
shablonda qotirilgan matn tugunlari (`template/i18n`).

**O'chgan (ko'chirilmaydi):** `auth/login`, `auth/register` (D5, D9 — Identity), `portal/*`,
`agent-portal/*` (D8), `upgrade-banner`, `mustChangePasswordGuard`, `tenant-slug.util`,
`must-change-password`/parol almashtirish (profil va foydalanuvchilar ekranida ham).

## Marshrutlar

Guard'lar: **M** = `moduleGuard`, **P** = `permissionGuard`, **F** = `featureGuard`.
Bo'lim guard'i ota marshrutda (`app.routes.ts`), bola guard'i `features/*.routes.ts` da.

| Marshrut | Guard | Eski komponent (`wms-ui/src/app/modules/…`) |
|---|---|---|
| `/dashboard` | — | `dashboard/dashboard.component` |
| `/warehouse` | M `WAREHOUSE_RAW` + P `warehouse.view` | `warehouse/stock-overview` |
| `/warehouse/warehouses` | ↑ | `warehouse/warehouse-list` |
| `/warehouse/movements` | ↑ | `warehouse/movements` |
| `/warehouse/locations` | ↑ + F `warehouse.locations` | `warehouse/locations` |
| `/warehouse/batches` | ↑ + F `warehouse.batches` | `warehouse/batches` |
| `/production` → `orders` | M `PRODUCTION` + P `production.view` | — |
| `/production/stages` | ↑ + F `production.stages` | `production/stages` |
| `/production/recipes`, `/new`, `/:id` | ↑ + F `production.recipes` | `production/recipe-list`, `recipe-create`, `recipe-detail` |
| `/production/orders`, `/new`, `/:id` | ↑ + F `production.orders` | `production/order-list`, `order-create`, `order-detail` |
| `/transfers`, `/new`, `/:id` | M `TRANSFERS` + P `transfers.view` | `transfers/transfer-list`, `transfer-create`, `transfer-detail` |
| `/finance` | M `FINANCE` + P `finance.view` | `finance/summary` |
| `/finance/transactions` | ↑ + F `finance.transactions` | `finance/transactions` |
| `/finance/debts` | ↑ + F `finance.debts` | `finance/debts` |
| `/finance/payments` | ↑ + F `finance.payments` | `finance/payments` |
| `/kpi` | M `KPI` + P `kpi.view` | `kpi/dashboard` |
| `/kpi/shifts` | ↑ + F `kpi.shifts` | `kpi/shifts` |
| `/kpi/plans`, `/kpi/actuals` | ↑ + F `kpi.plans` | `kpi/plans`, `kpi/actuals` |
| `/kpi/attendance` | ↑ + F `kpi.attendance` | `kpi/attendance` |
| `/counterparties` → `suppliers` | P `partners.view` | — |
| `/counterparties/suppliers` | ↑ + F `counterparties.suppliers` | `counterparties/supplier-list` |
| `/counterparties/clients` | ↑ + F `counterparties.clients` | `counterparties/client-list` |
| `/counterparties/:id` | ↑ | `counterparties/detail` |
| `/agents`, `/agents/:id` | M `AGENTS` + P `agents.view` | `agents/agent-list`, `agent-detail` |
| `/products`, `/categories`, `/units` | P `products.view` | `products/product-list`, `categories`, `units` |
| `/delivery` | M `DELIVERY` + P `delivery.view` (+ bola P `delivery.view`) | `delivery/deliveries` |
| `/delivery/new` | ↑ + P `delivery.manage` | `delivery/delivery-create` |
| `/delivery/vehicles`, `/drivers`, `/:id` | ↑ | `delivery/vehicles`, `drivers`, `delivery-detail` |
| `/settings` → `profile` | — | — |
| `/settings/users` | P `settings.users` | `settings/users` (D7: yaratish/parol yo'q) |
| `/settings/roles` | P `settings.roles` | `settings/roles` |
| `/settings/modules` | P `settings.modules` | `settings/modules` (faqat ko'rish, D6) |
| `/settings/subscription` | — | `settings/subscription` (+ `shared/components/limit-notice`) |
| `/settings/qc-parameters` | F `qc.parameters` + P `quality.view` | `settings/qc-parameters` |
| `/settings/audit` | P `audit.view` | `settings/audit-log` |
| `/settings/profile` | — | `settings/profile` (parolsiz, D5) |
| `/custom/example-feature` | F `custom.example-feature` | `custom/example-feature` |

Qobiq sahifalari (tayyor): `/login`, `/auth/callback`, `/subscription` (402 bloki),
`/select-tenant` (tenant noma'lum/biriktirilmagan), `/forbidden`, `/not-in-plan`.
