# WMS — Backend arxitekturasi (`wms-api`)

> **Oxirgi yangilanish:** 2026-08-04 · **Branch:** `saas-admin`
> **Holat:** SaaS majburlash (T1–T13), soddalashtirilgan model (S1–S7) va brendlash +
> limit ogohlantirishi (B1–B2) bajarilgan — build 0 xato / 0 ogohlantirish,
> 54/54 va 31/31 uchma-uch sinov.
> Umumiy loyiha qoidalari va qolgan ishlar: **`CLAUDE.md`** · Frontend: **`FRONTEND.md`**

---

## 1. Texnologiya va joylashuv

| Qatlam | Texnologiya |
|---|---|
| Runtime | .NET 8 LTS (`global.json` bilan qulflangan) |
| Web | ASP.NET Core Web API — port **7040** |
| ORM | Entity Framework Core 8 |
| DB | SQLite (MVP), PostgreSQL-ready (faqat connection string o'zgaradi) |
| Auth | JWT Bearer (7 kunlik token) |
| Log | Serilog (structured) |
| Doc | Swagger / OpenAPI |

Bitta backend **uchta** frontendga xizmat qiladi: `wms-ui` (tenant, `/api/*`),
`wms-admin` (SuperAdmin, `/api/admin/*`), portallar (`/api/portal/*`, `/api/agent-portal/*`).

---

## 2. Solution tuzilmasi (Clean Architecture)

```
wms-api/
├── WMS.sln · global.json          # .NET 8 SDK qulfi — o'chirmang
├── WMS.Domain/                    # Entity + Enum. Hech narsaga bog'liq emas
│   ├── Common/BaseEntity.cs
│   ├── Entities/                  # 41 fayl
│   └── Enums/                     # 18 fayl
├── WMS.Application/               # Shartnomalar — Domain'ga bog'liq
│   ├── Common/                    # ApiResponse, AppException, ModuleCodes,
│   │                              # SubscriptionOptions, SubscriptionPolicy, PhoneHelper
│   ├── Interfaces/                # 27 servis interfeysi
│   └── DTOs/                      # 21 papka (Auth, Subscription, Plans, ...)
├── WMS.Infrastructure/            # Implementatsiya — Application'ga bog'liq
│   ├── Persistence/               # WmsDbContext, DataInitializer, TenantProvisioner,
│   │                              # PlanModules, PlanLimits
│   ├── Migrations/                # 15 migration
│   └── Services/                  # 29 servis (3 tasi BackgroundService)
└── WMS.API/                       # Kirish nuqtasi
    ├── Controllers/               # 24 controller
    ├── Middleware/                # 5 fayl (quyida)
    └── Program.cs                 # DI, JWT, policy, CORS, rate limit, pipeline
```

**Bog'liqlik yo'nalishi:** `API → Infrastructure → Application → Domain`. Teskarisi yo'q.

---

## 3. Ma'lumotlar modeli

### 3.1 Asos

```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;   // soft delete
}
```

- **Global soft-delete filtri** — `OnModelCreating` da barcha `BaseEntity` uchun
  `HasQueryFilter(e => !e.IsDeleted)`. **Hech qachon hard delete qilinmaydi.**
- **Multi-tenancy** — deyarli har entity'da `TenantId`. Controller uni **faqat JWT'dan**
  oladi (`BaseController.TenantId`), so'rov tanasidan **hech qachon** emas.
- **SQLite decimal** — barcha `decimal` xossalar `HasConversion<double>()` orqali `double`
  sifatida saqlanadi. Pul agregatlari uchun client-side yig'indini afzal ko'ring.

### 3.2 42 DbSet — mavzular bo'yicha

| Bo'lim | Entity'lar |
|---|---|
| **Tenant & auth** | `Tenant`, `User`, `Role`, `UserRole`, `Permission`, `RolePermission`, `Module`, `TenantModule`, **`Plan`** |
| **Mahsulot** | `Category` (daraxt), `Unit`, `Product` |
| **Kontragent** | `Counterparty` (Supplier/Client/Both + portal kirish) |
| **Agent** | `Agent`, `CommissionRecord` |
| **Ombor** | `Warehouse`, `Location`, `Batch`, `WarehouseStock`, `Transfer`, `TransferItem` |
| **Delivery** | `Vehicle`, `Driver`, `Delivery`, `DeliveryStop` |
| **Ishlab chiqarish** | `ProductionStage`, `ProductionRecipe`, `RecipeStage`, `RecipeStageItem`, `ProductionOrder`, `StageExecution` |
| **Sifat** | `QcParameter`, `QcCheck` |
| **Moliya** | `Transaction`, `Debt`, `PaymentHistory` |
| **KPI** | `Shift`, `ShiftPlan`, `ShiftActual`, `AttendanceLog` |
| **Tizim** | `Notification`, `AuditLog` |

### 3.3 Enum'lar (18)

`ProductType`, `CounterpartyType`, `WarehouseType`, `TransferType`, `TransferStatus`,
`ReturnReason`, `ProductionOrderStatus`, `StageExecutionStatus`, `QcParameterType`,
`TransactionType`, `PaymentMethod`, `PaymentDirection`, `AttendanceMethod`,
`NotificationType`, `CommissionStatus`, `DeliveryStatus`, `DeliveryStopStatus`,
**`SubscriptionStatus`** (`Trial=1, Active=2, Suspended=3`).

### 3.4 Indekslar

13 ta performance indeksi + **2 ta unique (qisman)**:
`Tenant.Slug` va `Plan.Code` — `WHERE IsDeleted = 0` sharti bilan.

---

## 4. SaaS control plane

### 4.1 Modul tizimi

11 ta modul (`Module` jadvali, `ModuleCodes` konstantalari):

```
WAREHOUSE_RAW(1) PRODUCTION(2) WAREHOUSE_FINISHED(3) TRANSFERS(4) FINANCE(5)
KPI(6) SUPPLIERS(7) CLIENTS(8) QUALITY(9) AGENTS(10) DELIVERY(11)
```

`TenantModule` — tenant bo'yicha yoqilgan/o'chirilgan holat.
**Faqat SuperAdmin o'zgartira oladi** (`PUT /api/tenants/{id}/modules` → SuperAdmin policy).

### 4.2 Plan

Platforma-global entity (**`TenantId` yo'q — hech qachon qo'shmang**):

| Maydon | Ma'nosi |
|---|---|
| `Name`, `Code`, `Price`, `IsActive` | Asosiy |
| `ModuleCodes` | CSV — planga kiruvchi modullar |
| `FeatureCodes` | CSV — planga kiruvchi feature'lar (modul ichidagi sahifalar) |
| `MaxUsers`, `MaxWarehouses`, `MaxTransfersPerMonth` | Limitlar |
| `TrialDays` | Sinov uzunligi (0 = pullik plan) |
| `IsDefault` | Self-service registratsiya shu planga bog'lanadi (bittagina) |

Seed (`DataInitializer.SeedPlansAsync` — mavjudini **hech qachon qayta yozmaydi**):

| Plan | Kod | Narx | Modul | Users / Ombor / Transfer(oy) | Trial |
|---|---|---|---|---|---|
| Trial | `trial` | 0 | 11 | 3 / 2 / 200 | 14 kun, **default** |
| Basic | `basic` | 1 200 000 | 5 | 5 / 3 / 1 000 | — |
| Pro | `pro` | 2 900 000 | 8 | 25 / 10 / 10 000 | — |
| Enterprise | `enterprise` | 5 900 000 | 11 | 200 / 50 / 100 000 | — |

**Tenant** qo'shimcha maydonlari: `PlanId` (FK, plan o'chsa `SetNull`), `PlanType`
(plan kodining nusxasi), `SubscriptionStatus`, `TrialEndsAt`, **`PaidUntil`** (manual billing),
**`SuspendReason` / `SuspendNote` / `SuspendPublicMessage` / `SuspendedUntil` / `SuspendedAt` /
`SuspendedByUserId`**, **`OrganizationId`**.

> ⚠️ **Asosiy kelishuv:** plani **bor** tenant → plan modullari va limitlari;
> plani **yo'q** tenant → **cheksiz**. Bu ongli qaror — mavjud mijozlarning ishi
> to'satdan to'xtamasligi uchun.

### 4.3 Majburlash mexanizmlari

| Mexanizm | Fayl | Nima qiladi |
|---|---|---|
| `RequireModuleAttribute` | `WMS.API/Middleware/` | Modul yoqilmagan bo'lsa **403** + `code: module_disabled:CODE`. Bir nechta kod berilsa "biror biri" semantikasi (Counterparties → SUPPLIERS **yoki** CLIENTS) |
| `SubscriptionEnforcementMiddleware` | `WMS.API/Middleware/` | Har autentifikatsiyalangan so'rovda tenant holatini tekshiradi → **402** |
| `SubscriptionPolicy` | `WMS.Application/Common/` | Yagona qaror nuqtasi — login ham, middleware ham shuni chaqiradi |
| `ITenantStateService` | `Infrastructure/Services/` | Tenant holatini **60 s** cache qiladi; suspend/activate/plan/modul yozuvida cache **darhol** tozalanadi |
| `RequireFeatureAttribute` | `WMS.API/Middleware/` | Feature yoqilmagan bo'lsa **403** + `code: feature_disabled:CODE` (moduldan bir pog'ona mayda) |
| `FeatureResolver` | `Infrastructure/Persistence/` | Feature holatini yechadi: tenant override → plan → katalog default, modul veto bilan |
| `PlanLimits` | `Infrastructure/Persistence/` | `UserService`/`WarehouseService`/`TransferService`/`ImportService` yaratishda limitni tekshiradi → **402** |
| `RequirePermissionAttribute` | `WMS.API/Middleware/` | RBAC — permission kodi bo'yicha |
| `AuditLogFilter` | `WMS.API/Middleware/` | Yozuvchi amallarni `AuditLog` ga yozadi |

**Enforcement'dan ozod:** `/api/auth`, `/api/subscription`, `/api/admin`, `/health`,
`/swagger` va **SuperAdmin**. `/api/subscription` ataylab ozod — bloklangan mijoz
sababni ko'ra olishi shart.

**Gate'dan ataylab ochiq qoldirilgan:** `analytics/summary`, `analytics/dashboard-summary`,
`analytics/monthly-comparison`, `analytics/products/distribution`, `export/products`,
`import/products`, `import/users` — bular tenantning yig'ma/asosiy ma'lumoti, modul sahifasi emas.

### 4.4 Enum'lar simda qanday yuradi

| Guruh | Shakl | Nega |
|---|---|---|
| **Platforma** — `SuspendReason`, `PlatformPaymentMethod`, `LeadStatus`, `LeadSource` | **Nom** (`"ClientRequest"`, `"BankTransfer"`, `"Won"`) | Shartnomada shunday kelishilgan; `wms-admin` aynan nom yuboradi va nom kutadi |
| **Tenant** — `SubscriptionStatus`, `TransferStatus`, `CounterpartyType`, va h.k. | **Raqam** (1/2/3) | Mavjud barcha ekranlar shunga qurilgan — o'zgartirilsa hammasi buziladi |

Nom shakli DTO xossasiga `[JsonConverter(typeof(JsonStringEnumConverter))]` qo'yish orqali
beriladi (global emas — global qilinsa tenant tomoni sinadi).

### 4.5 Xato kodlari (`ApiResponse.Code`)

| HTTP | `code` | Sabab |
|---|---|---|
| 403 | `module_disabled:PRODUCTION` | Modul planga kirmagan |
| 403 | `feature_disabled:production.recipes` | Feature yoqilmagan (S4) |
| 402 | `trial_expired` | Sinov muddati + grace tugagan |
| 402 | `payment_expired` | To'lov muddati (`PaidUntil` + grace) tugagan (S1) |
| 402 | `suspended_nonpayment` / `suspended_request` / `suspended_technical` / `suspended_violation` / `suspended_other` | To'xtatilgan — sababi bo'yicha (S2) |
| 402 | `tenant_inactive` | Tenant o'chirilgan / faolsiz |
| 402 | `limit_users` / `limit_warehouses` / `limit_transfers` | Plan limiti to'lgan |

`POST /api/auth/login` bloklangan holatda **402** qaytaradi (avval 400 edi).

### 4.6 Provizatsiya va trial oqimi

`TenantProvisioner.ProvisionAsync` — tenant yaratishning **yagona yo'li**
(self-service register ham, SuperAdmin create ham):
tenant → default plan modullari → barcha ruxsatli Admin roli → admin user.
Slug formati va **24 ta zaxira slug** qora ro'yxati tekshiriladi.
`TrialEndsAt = UtcNow + TrialDays` albatta qo'yiladi.

`SubscriptionExpiryBackgroundService` — kuniga bir marta uchta ishni bajaradi:
muddati (+grace) o'tgan trial'lar → `Suspended`; `PaidUntil` (+grace) o'tgan pullik
tenantlar → `Suspended` (`NonPayment`); `SuspendedUntil` sanasi kelganlar → avtomatik
`Active` (to'lovi ham o'tgan bo'lsa yoqilmaydi). **Ma'lumot hech qachon o'chirilmaydi.**

### 4.7 Manual billing (S1)

Avtomat to'lov (CLICK/Payme) yo'q — pul qo'lda qabul qilinadi va SuperAdmin qayd etadi.

- **`PaymentRecord`** (platforma darajasi): `TenantId`, davr (`PeriodStart`/`PeriodEnd`),
  summa, valyuta, usul (`Cash`/`BankTransfer`/`Card`/`Other`), izoh, kim qayd etgani.
- To'lov qayd etilsa `Tenant.PaidUntil` **oldinga suriladi** (orqaga hech qachon emas),
  `NonPayment` sababli suspend bo'lgan tenant **avtomat tiklanadi**, trial esa `Active` ga o'tadi.
- Yozuvni bekor qilish (soft delete) `PaidUntil` ni qolgan yozuvlar bo'yicha qayta hisoblaydi.
- `PaidUntil = null` → **to'lov hech qachon bloklamaydi** (eski mijozlar va tizim tenanti).

```
POST   /api/admin/tenants/{id}/payments      to'lovni qayd etish
GET    /api/admin/tenants/{id}/payments      tarix
DELETE /api/admin/payments/{paymentId}       xato yozuvni bekor qilish
GET    /api/admin/tenants/expiring?days=7    muddati tugayotganlar
```

### 4.8 Feature qatlami (S4/S5)

Uch qatlam: **Plan** (nima sotildi) → **Feature** (bu tenantda bormi) → **Permission**
(tenant ichida kim ishlatadi).

- **`Feature`** — platforma katalogi (29 ta seed): `Code`, `Name`, `ModuleCode?`,
  `DefaultEnabled`, `SortOrder`, `IsCustom`, `OwnerTenantId?`, `Reason?`.
  `ModuleCode = null` — modulga tegishli bo'lmagan (export, import, analytics).
- **`TenantFeature`** — tenant bo'yicha override.
- **Yechim tartibi:** override → plan `FeatureCodes` → katalog `DefaultEnabled`;
  va har doim: **moduli o'chiq feature ham o'chiq** (`source: "module"`).
- Holat `ITenantStateService` cache'ida modullar bilan **birga** saqlanadi.

```
GET  /api/admin/features?isCustom=          katalog
POST /api/admin/features                     yangi (custom qoidalari validatsiyada)
GET  /api/admin/tenants/{id}/features        yechilgan holat + manbasi
PUT  /api/admin/tenants/{id}/features        override (isEnabled: null → override o'chadi)
```

**Custom feature qoidalari** (kod darajasida majburlanadi): kodi `custom.` bilan boshlanadi,
`IsCustom = true` → `DefaultEnabled = false`, `OwnerTenantId` majburiy, planga qo'shib
bo'lmaydi. Skelet: `WMS.Application/Features/Custom/`, `WMS.API/Controllers/Custom/`.
Reestr: `docs/CUSTOM_FEATURES.md`.

### 4.9 Lead oqimi (S3/S7)

Self-service registratsiya **yopiq**: `Registration:SelfServiceEnabled = false` (default) da
`POST /api/auth/register` → **404**. Kod o'chirilmagan — lead konversiyasi aynan shu
provizatsiya yo'lidan foydalanadi.

- **`Lead`** (platforma darajasi): kompaniya, aloqa, telefon, manba (`Website`/`Portal`/
  `Manual`/`Referral`), `ReferrerTenantId?`, holat (`New`→`Contacted`→`DemoGiven`→`Won`/`Lost`),
  `ConvertedTenantId?`.
- `POST /api/leads` — anonim, rate limit **5/soat/IP** (`leads` policy). Bir xil telefon
  24 soat ichida takrorlansa yangi yozuv yaratilmaydi (200 qaytadi).
- Portal foydalanuvchilari (`POST /api/portal/upgrade-interest`,
  `POST /api/agent-portal/upgrade-interest`) — 30 kunlik dublikat oynasi,
  `ReferrerTenantId` to'ldiriladi. Bu endpointlar obuna enforcement'idan **ozod emas**.
- `POST /api/admin/leads/{id}/convert` — tenant yaratadi, lead `Won` bo'ladi.

### 4.10 Brendlash (B1)

Har mijoz o'z logosi va rangini ko'radi, **yagona build** saqlanadi: brendlash — ma'lumot,
kod emas. Chegara qat'iy: **keng logo + kvadrat logo + bitta rang**. "Sidebar joylashuvi",
"menyu tartibi" kabi so'rovlar brendlash emas.

| Maydon | Izoh |
|---|---|
| `Tenant.LogoUrl` | Keng logo — sidebar ochiq, login sahifasi, hisobot sarlavhasi |
| `Tenant.LogoSquareUrl` | Kvadrat — sidebar yig'ilgan, favicon |
| `Tenant.BrandColor` | `#RRGGBB`; palitrani frontend hosil qiladi |

```
POST   /api/admin/tenants/{id}/logo?type=wide|square    multipart/form-data (SuperAdmin)
DELETE /api/admin/tenants/{id}/logo?type=wide|square
GET    /api/admin/tenants/{id}/branding
GET    /api/public/branding?slug=                        anonim, 30/daqiqa/IP
```

**Validatsiya:** SVG / PNG / WebP · ≤ **512 KB** · keng ≤ 600×200 px, kvadrat ≤ 512×512 px ·
SVG ichida `<script>`, `on*=`, `javascript:` yoki tashqi havola bo'lsa **rad etiladi**
(tozalash emas — yarim ishlaydigan SVG'dan aniq xato yaxshiroq).
O'lcham `ImageInspector` bilan fayl sarlavhasidan o'qiladi (kutubxonasiz).

**Saqlash:** `wwwroot/uploads/tenants/{id}/logo-wide-<hash>.<ext>` — fayl nomidagi hash
brauzer cache'ini buzadi, eski fayl almashtirilgach o'chiriladi. Papka **ishga tushishdan
oldin** yaratiladi, aks holda yangi deploy'da static file middleware o'chib qoladi.
Yo'l autentifikatsiyasiz ochiq: logo maxfiy emas va login sahifasida kerak.

**Bir xil `branding` obyekti uch joyda:** login javobida (`data.branding`),
`GET /api/subscription/me` da va public endpointda — mijozda bitta mapping.

**Hisobotlar:** `ReportBranding` PDF (QuestPDF) va Excel (ClosedXML) sarlavhasiga logo va
nomni qo'yadi. Har qadam best-effort: logo yo'q bo'lsa faqat nom, o'qib bo'lmasa faqat nom —
bezak tufayli hisobot yiqilmaydi. SVG PDF'ga qo'yilmaydi (raster kerak).

`BrandColor` oddiy `PUT /api/admin/tenants/{id}` orqali; bo'sh satr — standart temaga qaytaradi.

### 4.11 Limit ogohlantirishi (B2)

Mijoz limitga **urilgunicha** bilsin. `Subscription:LimitWarnPercent` (default 80) endi
ishlatiladi:

- `ApiResponse` ga ixtiyoriy **`warning`** (`{ code, message }`) qo'shildi. Bu **xato emas**:
  HTTP 200/201 va `success: true` o'zgarmaydi, eski mijoz kodi buzilmaydi.
- Kodlar: `limit_warn_users` · `limit_warn_warehouses` · `limit_warn_transfers`.
  Kod tarjima qilinmaydi, xabar `Accept-Language` bo'yicha tarjima qilinadi.
- Mexanizm: servis yaratishdan keyin `PlanLimits.ReportUsageAsync` chaqiradi →
  scoped `IRequestWarnings` ga yozadi → `ResponseLocalizationFilter` javobga qo'shadi.
- `/api/subscription/me` limitlariga `users` / `warehouses` / `transfers` obyektlari qo'shildi:
  `max`, `current`, `usagePercent`, `isNearLimit`. Plansiz tenantda oxirgi ikkitasi **null**.
  Foiz endi **faqat backendda** hisoblanadi (ikki joyda hisob vaqt o'tib ajraladi).

> Limitga yetganda xatti-harakat o'zgarmadi: avvalgidek **402 `limit_*`**.

### 4.12 Organization (S6)

`Counterparty` — bitta tenantning **ichki yozuvi**; `Organization` — platforma darajasidagi
**haqiqiy kompaniya**. Bog'lash **faqat INN (STIR, 9 raqam)** bo'yicha:
`OrganizationMatcher.ResolveAsync` (counterparty CRUD, Excel import, tenant provizatsiyasi).

> ⚠️ **Maxfiylik:** tenantlar uchun `organizations` endpointi **yo'q va bo'lmaydi** —
> aks holda mijozlar bir-birining mijozlar bazasini yig'ib olardi. Faqat SuperAdmin:
> `GET /api/admin/organizations`, `GET /api/admin/organizations/{id}`.

---

## 4b. Tillar — javob matnlari (uz / ru / en)

Backend qaytaradigan **har bir odam o'qiydigan matn** uch tilda: xato xabarlari, obuna
bloki sabablari, limit ogohlantirishlari va "O'chirildi" kabi tasdiqlar.

**Qanday ishlaydi:**

1. Kod ichida xabar **inglizcha** yoziladi — `throw new AppException("Tenant not found")`.
2. O'sha inglizcha matn **tarjima kaliti** bo'lib xizmat qiladi:
   `WMS.Application/Common/Localization/Translations.cs` da `[English] = { uz, ru }`.
3. Til **`Accept-Language`** sarlavhasidan aniqlanadi (`Lang.FromHeader`, q-qiymatlar bilan).
   `uz-cyrl` → `uz`, noma'lum til yoki sarlavhasiz so'rov → default (`uz`).
4. Tarjima **chekkada** qilinadi: `ExceptionHandlingMiddleware`,
   `SubscriptionEnforcementMiddleware`, `RequireModule`/`RequireFeature`/`RequirePermission`
   atributlari va muvaffaqiyat xabarlari uchun `ResponseLocalizationFilter`.

**Nega kalit sifatida inglizcha matn:** mavjud ~100 ta `throw` joyini o'zgartirmaslik uchun.
Tarjimasi yo'q xabar inglizcha qaytadi — mijoz inglizcha o'qishi kichik muammo,
xabarning umuman yo'qolishi katta muammo.

**Argumentli xabarlar** (`Messages.LimitUsers` va h.k.) shablon + qiymat sifatida uzatiladi,
matn tayyor holda emas — aks holda kalit buzilardi:

```csharp
throw new PaymentRequiredException("limit_users", Messages.LimitUsers, plan.Name, plan.MaxUsers);
// uz: "Tarifingiz (Trial) 2 ta foydalanuvchiga ruxsat beradi..."
// ru: "Ваш тариф (Trial) допускает 2 пользователей..."
```

**O'zgarmaydigan narsalar:**
- `ApiResponse.Code` (`payment_expired`, `feature_disabled:CODE`, …) — **hech qachon
  tarjima qilinmaydi**, u mashina uchun.
- SuperAdmin yozgan `SuspendPublicMessage` — u odam yozgan matn, tegilmaydi.

**Frontend:** `wms-ui` va `wms-admin` da `language.interceptor` har so'rovga interfeys
tilini `Accept-Language` sifatida qo'shadi.

**Yangi til qo'shish:** `Lang.Supported` ga kod, `Translations` jadvaliga ustun.
**Yangi xabar qo'shish:** inglizcha yozing va jadvalga bitta qator qo'shing.

---

## 5. Autentifikatsiya va avtorizatsiya

### 5.1 Policy'lar (`Program.cs`)

| Policy | Talab qilinadigan claim | Kim ishlatadi |
|---|---|---|
| `MainApi` | `userId` | `BaseController` — barcha tenant endpointlari |
| `PortalOnly` | `counterpartyId` | Kontragent portali |
| `AgentPortalOnly` | `agentId` | Agent portali |
| `SuperAdmin` | `isSuperAdmin=true` | `AdminController`, cross-tenant amallar |

To'rt oqim bitta imzo kalitidan foydalanadi — token turi **claim shakli** bilan ajratiladi,
ya'ni portal tokeni asosiy API'ga o'ta olmaydi.

### 5.2 BaseController

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MainApi")]
public abstract class BaseController : ControllerBase
{
    protected int TenantId => int.Parse(User.FindFirst("tenantId")?.Value ?? "0");
    protected int UserId   => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    protected bool IsSuperAdmin => User.FindFirst("isSuperAdmin")?.Value == "true";
}
```

### 5.3 RBAC

`Permission` (seed qilingan kodlar) → `RolePermission` → `Role` → `UserRole` → `User`.
Endpoint darajasida `[RequirePermission("transfers.confirm")]`.
Delivery moduli 26/27-permissionlarni qo'shgan.

**Tizim tenanti:** id **1** (`WMS Admin`, slug `admin`).
Seed admin: telefon `+998901234567`, parol `Admin123456` (`Seed:AdminPassword` bilan almashtiriladi).

---

## 6. Controller xaritasi (24)

| Controller | Prefiks | Modul gate |
|---|---|---|
| `AuthController` | `/api/auth` | — (login, register, me, my-permissions) |
| `SubscriptionController` | `/api/subscription` | — (`me`, `plans`) |
| `AdminController` | `/api/admin` | SuperAdmin: tenants, plans, modules, stats |
| `TenantsController` | `/api/tenants` | `PUT modules` → SuperAdmin |
| `UsersController` | `/api/users` | limit: `MaxUsers` |
| `ProductsController` | `/api/products`, `/categories`, `/units` | — |
| `CounterpartiesController` | `/api/counterparties` | SUPPLIERS **yoki** CLIENTS |
| `AgentsController` | `/api/agents` | AGENTS |
| `WarehousesController` | `/api/warehouses`, `/locations`, `/batches` | WAREHOUSE_RAW · limit: `MaxWarehouses` |
| `TransfersController` | `/api/transfers` | TRANSFERS · limit: `MaxTransfersPerMonth` |
| `ProductionController` | `/api/production` | PRODUCTION |
| `DeliveryController` | `/api/delivery` | DELIVERY |
| `FinanceController` | `/api/finance` | FINANCE |
| `KpiController` | `/api/kpi`, `/shifts`, `/attendance` | KPI |
| `QcController` | `/api/qc` | QUALITY |
| `AnalyticsController` | `/api/analytics` | qisman (§4.3) |
| `ExportController` | `/api/export` | qisman |
| `ImportController` | `/api/import` | qisman |
| `NotificationsController` | `/api/notifications` | — |
| `AuditController` | `/api/audit` | — |
| `CurrencyController` | `/api/currency` | — |
| `PortalController` | `/api/portal` | `PortalOnly` |
| `AgentPortalController` | `/api/agent-portal` | `AgentPortalOnly` |

Batafsil endpoint ro'yxati — **Swagger** (`/swagger`), u yagona ishonchli manba.

---

## 7. Fon xizmatlari

| Servis | Nima qiladi |
|---|---|
| `DbBackupBackgroundService` | Kunlik SQLite `VACUUM INTO` snapshot |
| `BatchExpiryBackgroundService` | Muddati yaqinlashgan partiyalar bo'yicha bildirishnoma |
| `SubscriptionExpiryBackgroundService` | Muddati o'tgan trial'larni Suspended ga o'tkazish |

Qo'shimcha: `TelegramService` (config-gated), `TransferPdfService` / `DeliveryPdfService`
(yuk xati), `ExportService` / `ImportService` (Excel), `IAiAdvisorService` — **bo'sh stub**
(kelajakdagi AI Advisor uchun joy belgilangan).

---

## 8. Biznes qoidalari

**Transfer → zaxira** (tasdiqlanganda):
- `Incoming` — `WarehouseStock` ga qo'shiladi (Batch bo'lmasa yaratiladi)
- `Outgoing` — ayiriladi, `Batch.RemainingQuantity` yangilanadi
- `Internal` — manbadan ayirib, maqsadga qo'shiladi
- **FEFO** — olishda har doim muddati eng erta tugaydigan partiya

**Ishlab chiqarish → zaxira:** bosqich bajarilganda kirishlar ayiriladi;
`AllowWarehouseOutput = true` bo'lsa chiqish belgilangan omborga; oxirgi bosqichda
tayyor mahsulot omboriga + yangi `Batch` + `ProductionOutput` turidagi `Transfer`.

**Moliya → qarz:** chiquvchi transfer tasdiqlansa mijoz qarzi oshadi; kiruvchi tasdiqlansa
bizning qarzimiz; to'lov qayd etilsa muvofiq kamayadi.

**KPI:** `Samaradorlik % = Actual / Planned × 100` · `Brak % = Waste / Actual × 100`

---

## 9. Konfiguratsiya

```json
{
  "ConnectionStrings": { "Default": "Data Source=wms.db" },
  "Jwt":  { "Key": "<32+ belgi — prod'da env orqali>" },
  "Seed": { "AdminPassword": "<prod'da env orqali>" },
  "Registration": { "SelfServiceEnabled": false },   // public register yopiq
  "Support": { "Phone": "+998 ...", "Email": "..." }, // bloklangan mijozga ko'rsatiladi
  "Subscription": {
    "TrialDays": 14,          // default trial uzunligi (plan o'zi belgilamasa)
    "GraceDays": 3,           // trial tugagach necha kun ishlashda davom etadi
    "PaidGraceDays": 3,       // to'lov muddati tugagach shuncha kun
    "StateCacheSeconds": 60,  // suspend maksimal necha soniyada kuchga kiradi
    "WarnBeforeDays": 7,      // frontend banneri uchun
    "LimitWarnPercent": 80
  }
}
```

Env ko'rinishi: `Subscription__TrialDays=14`, `Jwt__Key=...`.
CORS: `localhost:7050`, `localhost:7060` + prod domenlar.
Rate limit: `auth` — IP bo'yicha **10/daqiqa**; `leads` — IP bo'yicha **5/soat**.

---

## 10. Konventsiyalar (qat'iy)

- Javob **har doim** `ApiResponse<T>` — `Ok(data)` / `Fail(message)` / `code`.
- `TenantId` **faqat JWT'dan**. So'rov tanasidagi tenantId'ga hech qachon ishonilmaydi.
- Soft delete — `IsDeleted = true`. Hard delete yo'q.
- Pul — **`decimal`**, hech qachon `float`/`double` (DB darajasidagi konversiya alohida masala).
- Sana — backendda **har doim UTC**.
- Xatolar — `AppException` (400) / `NotFoundException` (404) / `PaymentRequiredException` (402) /
  `ModuleDisabledException` (403) / `FeatureDisabledException` (403). Xom `Exception` tashlanmaydi.
- Controllerdagi `catch (Exception)` bloki obuna/huquq rad javoblarini **yutmasligi** kerak —
  ular oldin `throw` qilinadi, aks holda 402/403 jimgina 400 ga aylanadi (bu xato bir marta yuz bergan).
- Pagination — barcha ro'yxat endpointlari `?page=1&pageSize=20`.
- Hisoblanadigan xossalarga `[NotMapped]` (`TotalPrice`, `EfficiencyPercent`, `WastePercent`).
- Modul seed id'lari 1–11 — **qayta seed qilinmaydi**. Feature'lar kod bo'yicha
  idempotent qo'shiladi (mavjudi hech qachon qayta yozilmaydi).
- `SuspendNote` — **ichki**; mijozga ko'rinadigan javoblarga hech qachon chiqmaydi
  (mijozga faqat `SuspendPublicMessage`).
- **Plan hech qachon `TenantId` olmaydi.**
- Yangi plan/limit imkoniyati qo'shsangiz — **server tomonidagi majburlashini ham** qo'shing.
  Hozirgi holat aynan shu qadam tashlab ketilgani uchun yuzaga kelgan edi.

---

## 11. Build va ishga tushirish

```bash
# Dev
cd wms-api
dotnet run --project WMS.API              # http://localhost:7040 · /swagger

# Build — API jarayoni ishlab tursa bin/ qulflanadi (MSB3027)
dotnet build WMS.sln -p:OutDir="<temp>\" -v q

# Smoke test (alohida port va baza bilan)
ASPNETCORE_URLS=http://localhost:7041 \
ConnectionStrings__Default="Data Source=<temp>.db" \
Jwt__Key="<32+ belgi>" ASPNETCORE_ENVIRONMENT=Production \
dotnet <temp>/WMS.API.dll

# Prod
dotnet publish WMS.API -c Release -o /var/www/wms-api
```

Migratsiyalar startupda avtomat qo'llanadi (`db.Database.Migrate()`).
`/health` — health check, Serilog — structured log.

> ⚠️ **Deploydan oldin bazani zaxiralang.** Oxirgi migration (`AddSaasEnforcement`)
> unique indeks qo'yadi va dublikat sluglarni `-dup<Id>` bilan qayta nomlaydi.
