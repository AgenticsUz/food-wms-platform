# WMS — hozirgi holat (qisqa xarita)

> **Sana:** 2026-09-02 · **Branch:** `saas-admin` · Manba: `docs/CLAUDE.md`, `BACKEND.md`,
> `FRONTEND.md` va kod. Bu fayl — tezkor ma'lumotnoma; tafsilot o'sha uch hujjatda.
> Uchta ilova, bitta backend: `wms-ui` (tenant, 7050, `/api/*`) · `wms-admin` (SuperAdmin,
> 7060, `/api/admin/*`) · `wms-api` (7040).

---

## 1. Plan → Feature → Permission modeli

**Uch qatlam:** Plan (nima sotildi) → Modul/Feature (bu tenantda yoqiqmi) → Permission
(tenant ichida kim ishlatadi).

| Jadval | Vazifasi |
|---|---|
| `Plan` | Platforma-global (`TenantId` **yo'q**). `Code`, `Price`, `ModuleCodes` (CSV), `FeatureCodes` (CSV), `MaxUsers` / `MaxWarehouses` / `MaxTransfersPerMonth`, `TrialDays`, `IsDefault`. Seed: `trial` (default, 14 kun) · `basic` · `pro` · `enterprise` |
| `Module` / `TenantModule` | 11 ta modul (`WAREHOUSE_RAW`, `PRODUCTION`, `TRANSFERS`, `FINANCE`, `KPI`, `SUPPLIERS`, `CLIENTS`, `QUALITY`, `AGENTS`, `DELIVERY`, `WAREHOUSE_FINISHED`); tenant bo'yicha yoqilgan/o'chirilgan holat |
| `Feature` / `TenantFeature` | 27–29 ta feature katalogi (modul ichidagi mayda dona: `production.recipes`, `finance.debts`, `export`…). `TenantFeature` — tenant override. `custom.*` — bir mijoz uchun, planga qo'shilmaydi |
| `Tenant` | `PlanId` (FK), `PlanType`, `SubscriptionStatus` (Trial/Active/Suspended), `TrialEndsAt`, `PaidUntil`, suspend maydonlari, brendlash maydonlari |
| `Permission` → `RolePermission` → `Role` → `UserRole` → `User` | Klassik RBAC, permission kodlari seed qilingan. `PermissionModules` mapping: `WAREHOUSE`→2 modul, `PARTNERS`→2 modul, `DASHBOARD`/`PRODUCTS`/`SETTINGS` — modulsiz, doim mavjud |

**Yechim tartibi (feature):** `TenantFeature` override → plan `FeatureCodes` → katalog
`DefaultEnabled`; moduli o'chiq bo'lsa feature ham o'chiq (modul veto).
**Kelishuv:** plani bor tenant → plan modullari/limitlari; plani **yo'q** tenant → **cheksiz**.

**Backend tekshiruv joyi** (`WMS.API/Middleware/`):

| Mexanizm | Natija |
|---|---|
| `RequireModuleAttribute` | 403 `module_disabled:CODE` |
| `RequireFeatureAttribute` (+ `FeatureResolver`) | 403 `feature_disabled:CODE` |
| `RequirePermissionAttribute` | 403 (RBAC) |
| `SubscriptionEnforcementMiddleware` → `SubscriptionPolicy.Evaluate` | har so'rovda 402 (`trial_expired`, `payment_expired`, `suspended_*`, `tenant_inactive`) |
| `PlanLimits` (User/Warehouse/Transfer/Import servislarida) | 402 `limit_users` / `limit_warehouses` / `limit_transfers`; 80% da `warning: limit_warn_*` |

Holat `ITenantStateService` da 60 s kesh, admin yozuvida darhol tozalanadi. Ozod yo'llar:
`/api/auth`, `/api/subscription`, `/api/admin`, `/health`, SuperAdmin.

**Frontend yashirish (`wms-ui`):**
- Sidebar `visibleNavItems` computed: `moduleCode` → `TenantService.isModuleEnabled`,
  `featureCode` → `FeatureService.isEnabled`, `permissionCode` → `PermissionService.can`.
  Uchtasi ham o'tmasa menyu bandi umuman ko'rinmaydi (modul yuqori qatlam).
- Route'larda `[moduleGuard('X'), permissionGuard('x.view')]`, sahifa ichida `featureGuard`.
- `*hasPermission="'code'"` direktivasi tugmani DOM'dan olib tashlaydi.
- Rollar sahifasida tarifga kirmaydigan ruxsat yashirilmaydi — kulrang + `isAvailable=false`.
- Ma'lumot manbai: login javobi + `GET /api/subscription/me` (`enabledModules`, `enabledFeatures`,
  `limits`); ruxsatlar `GET /api/auth/my-permissions`.

**HRM (`agentics-hrm`) bilan farqi:**

| | WMS | HRM |
|---|---|---|
| Sotuv birligi | `Plan` (modul + feature + limit CSV) | `Product` → `ProductModule` → `TenantProduct` / `TenantModule`; alohida `PermissionCatalogEntry` |
| Feature qatlami | bor (`Feature`/`TenantFeature`, override) | yo'q — modul darajasida |
| Limitlar | `Plan` da 3 ta raqamli limit, 402 | yo'q |
| Ruxsatlar | JWT'da emas, `my-permissions` orqali; backendda `RolePermission` so'rovi | ADR-0010: tokenda emas, `hr.role_permission` dan yechiladi, **Redis** kesh; `PermissionPolicyProvider` + arxitektura testlari (`PermissionCoverageTests`) |
| Control plane | o'sha backend, `/api/admin/*` | alohida `Admin.Api` (`/admin-api`) + alohida Identity |
| Enforcement testi | avtomat test **yo'q** (R23) | integratsiya/arxitektura testlari bor |

---

## 2. Admin panel oqimi (`wms-admin` → `/api/admin`, `SuperAdmin` policy)

| Amal | Endpoint | UI qadam |
|---|---|---|
| Tenant yaratish | `POST tenants` `{name, slug, adminFullName, adminPhone, adminPassword, planId?, inn?, paidUntil?}` → `TenantProvisioner` (tenant + modullar + Admin roli + admin user + 6 birlik, `TrialEndsAt` qo'yiladi) | Tenants → **New** → forma → Save |
| Tahrirlash / trial | `PUT tenants/{id}` `{name, slug, isActive, planId, subscriptionStatus, trialEndsAt, paidUntil, inn, brandColor}` | Tenants → qator → Edit; `trialEndsAt` kalendar bilan uzaytiriladi |
| Plan berish | `PUT tenants/{id}/plan` `{planId}` (yoki `PUT tenants/{id}` ichida) | Edit formasida plan tanlash |
| Modul toggle | `GET/PUT tenants/{id}/modules` (plandan tashqarisi "outside plan" badge) | qator → Modules dialogi |
| Feature override | `GET/PUT tenants/{id}/features` `{features:[{code,isEnabled|null}]}` | qator → Features (Plan / On / Off) |
| To'xtatish | `PUT tenants/{id}/suspend` `{reason, note, publicMessage, until?}` — `until` kelganda `SubscriptionPolicy` o'zi o'tkazadi | qator → Suspend dialogi (sabab, ichki izoh, mijozga matn, sana) |
| Yoqish | `PUT tenants/{id}/activate` | qator → Activate |
| O'chirish | `DELETE tenants/{id}` (soft) yoki `isActive=false` → 402 `tenant_inactive` | Edit → IsActive / Delete |
| To'lov (manual) | `POST/GET tenants/{id}/payments`, `DELETE payments/{id}`; `PaidUntil` oldinga suriladi, `NonPayment` suspend o'zi ochiladi | qator → Payment / History |
| Lead → tenant | `POST leads/{id}/convert` | Leads → Convert |
| Parol tiklash | `GET tenants/{id}/users`, `POST tenants/{id}/reset-user-password` | qator → Reset password |
| Planlar CRUD | `GET/POST/PUT/DELETE plans` | Plans sahifasi |
| Muddati tugayotganlar | `GET tenants/expiring?days=7` | Dashboard |

**Trial:** provizatsiyada `TrialEndsAt = now + Plan.TrialDays` (default 14, `Subscription:TrialDays`),
grace 3 kun. `SubscriptionExpiryBackgroundService` kuniga bir marta: trial/paid muddati o'tganlar →
`Suspended`; `SuspendedUntil` kelganlar → `Active`. Public self-service registratsiya
**yopiq** (`Registration:SelfServiceEnabled=false` → 404); o'rniga `POST /api/leads`.

---

## 3. Tenant aniqlash

- **Backend Host'ga qaramaydi.** `TenantId` faqat JWT claim'idan (`BaseController.TenantId`).
  Subdomen middleware **yo'q**. Slug faqat `POST /api/auth/login` tanasida (`tenantSlug`) keladi.
- **Frontend** (`wms-ui/src/app/core/utils/tenant-slug.util.ts`): `slugFromHost()` →
  `sinov.wms.uz` → `sinov`; `www/app/admin/api`, `localhost`, IP — tenant emas; kamida 3 bo'lak
  kerak. Tartib: subdomen → `localStorage.tenantSlug` → `environment.tenantSlug` (`admin`).
  Subdomen bo'lsa login formasida "Tashkilot kodi" maydoni ko'rsatilmaydi.
- `GET /api/public/branding?slug=` anonim (30/daqiqa/IP), noma'lum slug ham 200 — login
  sahifasini subdomen bo'yicha brendlashga tayyor, hozir chaqirilmaydi.
- Slug qoidasi: `^[a-z0-9]([a-z0-9-]{1,48}[a-z0-9])?$`, 24 ta zaxira slug, unique indeks.
- **Wildcard nginx konfiguratsiyasi repoda yo'q.** `docs/CLAUDE.md` §6 da faqat
  `app.` / `admin.` uchun bitta server-blok qolipi (`/api` + `/uploads` proxy, SPA fallback).
  HRM'da ham wildcard yo'q — u alohida `hrm.` / `hrm-admin.` / `id.` xostlari bilan ishlaydi
  va tenantni JWT `tenant_id` dan oladi (`X-Tenant-Id` faqat platforma admin / dev).

---

## 4. Branding

| Nima | Qayerda saqlanadi | Qayerda qo'llanadi |
|---|---|---|
| `Tenant.LogoUrl` (keng, ≤600×200) | fayl `wwwroot/uploads/tenants/{id}/logo-wide-<hash>.<ext>` | sidebar (yoyilgan), PDF/Excel hisobot sarlavhasi |
| `Tenant.LogoSquareUrl` (≤512×512) | `.../logo-square-<hash>.<ext>` | sidebar (yig'ilgan), favicon |
| `Tenant.BrandColor` (`#RRGGBB`) | `Tenants` jadvali | `branding.service.ts` bitta rangdan palitra → `--p-primary-*` (PrimeNG Aura) + `--brand-primary*` |
| `Tenant.Name` | `Tenants` | tab sarlavhasi `«Nom» — WMS`, sidebar matni |

Endpointlar: `POST/DELETE /api/admin/tenants/{id}/logo?type=wide|square` (SVG/PNG/WebP,
≤512 KB, SVG skript tekshiruvi), `GET /api/admin/tenants/{id}/branding`, `BrandColor` — oddiy
`PUT tenants/{id}`. Bir xil `branding` obyekti: login javobi, `/api/subscription/me`,
`/api/public/branding`. `wms-ui` oxirgi brendni `localStorage.branding` da saqlab
`provideAppInitializer` da qo'llaydi (miltillash yo'q), logout'da tozalaydi.
Login sahifasi hozircha standart. Nginx'da `/uploads` proxy **shart**.
`wms-admin` da: tenant formasida BRENDLASH bo'limi (ikki logo, colorpicker, WCAG ogohlantirish,
jonli ko'rinish).

---

## 5. Excel import / eksport

**Bor.** Kutubxona — **ClosedXML 0.105** (server tomonda, `WMS.Infrastructure/Services/`).
Frontendda Excel kutubxonasi yo'q: `export.service.ts` / `import.service.ts` faqat blob
yuklab oladi / `FormData` yuboradi.

- **Eksport** (`ExportController`, `ExportService`): `GET /api/export/transfers|stock|transactions|products|counterparties` → `XLWorkbook` → `byte[]` (xlsx); `transfers/{id}/pdf` — QuestPDF.
  Sarlavhaga tenant logosi + nomi (`ReportBranding`, best-effort).
- **Import** (`ImportController`, `ImportService`): `POST /api/import/products|counterparties|users` (multipart) → `ImportResultDto` (qator bo'yicha xato ro'yxati); shablonlar `GET /api/import/template/{products|counterparties|users}`.
  Import `PlanLimits` (users) va `OrganizationMatcher` (INN) orqali o'tadi.
- `export/products`, `import/*` modul gate'dan ataylab ochiq.

---

## 6. Domen / deploy holati

**Hozir:** prod `https://warehouse-system.uz` (HRM deploy hujjati 2026-08-28 da tasdiqlagan:
shu VPS'da ishlab turgan prodakshn). Rejalashtirilgan xostlar: `app.warehouse-system.uz`
(wms-ui), `admin.warehouse-system.uz` (wms-admin), `api.` ixtiyoriy. Ikkala frontend
`apiUrl: '/api'` (nisbiy, same-origin, CORS ishlamaydi). Backend systemd, port 7040, SQLite
`/var/www/wms/api/wms.db`. CI: `.github/workflows/ci.yml` faqat build (`main`, `new-style`).
`wms-api/publish/` eskirgan — ishlatilmasin.

**`id.agentics.uz` (HRM Identity) ga o'tish uchun kerak bo'ladi:**

1. Backendda tashqi IdP: JWT validatsiyasini `id.agentics.uz` issuer/JWKS ga o'tkazish
   (hozir o'z HS256 kaliti `Jwt:Key`, `sstamp` claim bilan sessiya bekor qilish).
2. Claim moslashuvi: WMS `tenantId`/`userId`/`isSuperAdmin` (int) ↔ HRM `sub`/`tenant_id`
   (Guid)/`tenant_code`/`is_platform_admin`. `BaseController` va 4 policy (`MainApi`,
   `PortalOnly`, `AgentPortalOnly`, `SuperAdmin`) qayta yoziladi; portal/agent tokenlari
   uchun alohida qaror.
3. `User` jadvalini Identity foydalanuvchisi bilan bog'lash (`identity.user_tenant` ↔ `User.Id`),
   telefon+parol login va `MustChangePassword`/parol tiklash oqimlarini IdP'ga ko'chirish.
4. Frontend: `POST /api/auth/login` o'rniga OIDC redirect (`wms-ui`, `wms-admin`), token
   kalitlari (`token`/`adminToken`) va `auth.interceptor` moslashuvi; `tenantSlug` → `tenant_code`.
5. Domen: `wms.agentics.uz` / `wms-admin.agentics.uz` DNS + host nginx bloklari + certbot
   (HRM qolipi: `docker/host-nginx/`), CORS/`Cors:AllowedOrigins` yangilash, `/uploads` proxy.
6. Tenant subdomeni (R20) kerak bo'lsa: `*.wms.agentics.uz` wildcard DNS + wildcard sertifikat
   (DNS-01) + `server_name ~^(?<slug>.+)\.wms\.agentics\.uz$` — frontend tomoni tayyor.
7. Prod'da `Jwt__Key`, `Seed__AdminPassword`, `Support__Phone/Email` env orqali; deploydan
   oldin DB zaxira (unique indeks migratsiyasi).

---

## 7. Versiyalar

| Qatlam | Versiya |
|---|---|
| .NET | SDK 8.0.417 (`global.json`, `rollForward: latestFeature`), `net8.0`, EF Core 8 |
| DB | SQLite (`Microsoft.EntityFrameworkCore.Sqlite` 8.*), decimal → double; PostgreSQL-ready |
| Backend paketlar | ClosedXML 0.105.0 · QuestPDF 2026.2.4 · Serilog.AspNetCore 10.0.0 · Swashbuckle 9.0.1 · BCrypt.Net-Next 4.1.0 · JwtBearer 8.* |
| Angular | 21.2.8 (package.json `^21.2.0`), standalone + signals, zoneless — ikkala ilova |
| PrimeNG | 21.1.5 (`@primeng/themes` ^21.0.4, Aura) · PrimeIcons 7 |
| CSS / grafik | Tailwind 4.2.2 · ApexCharts + ng-apexcharts · Transloco |
| Node (CI) | 20 |
