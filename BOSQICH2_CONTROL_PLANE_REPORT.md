# Bosqich 2 — SaaS Control Plane: Bajarilgan ishlar hisoboti

> **Sana:** 2026-07-02
> **Branch:** `new-style` · **Repo:** `golibjon94/food-wms-platform`
> **Manba reja:** `SAAS_ROADMAP.md` → Bosqich 2 (Control Plane)

Bu hujjat SaaS boshqaruv qatlami (control plane) bo'yicha bajarilgan ishlarni yozadi:
qo'lsiz tenant qo'shish uchun SuperAdmin, self-service registratsiya va registratsiya
ochilishidan oldingi tenant izolyatsiya auditi.

---

## Umumiy natija

| Vazifa | Holat | Commit |
|---|---|---|
| Tenant izolyatsiya audit + tuzatishlar | ✅ tugadi | `46b32f5` |
| SuperAdmin backend | ✅ tugadi | `46b32f5` |
| Self-service registratsiya backend | ✅ tugadi | `a8dcc98` |
| Frontend — register + SuperAdmin UI | ✅ tugadi | `0e29552` |

Barcha qismlar **build 0 xato / 0 warning** va backend endpointlari haqiqiy server bilan
sinovdan o'tkazildi. Hammasi `origin/new-style` ga push qilindi.

---

## 1. Tenant izolyatsiya audit (№1 xavfsizlik ishi)

Roadmap: *"Registratsiya ochilishidan oldin har endpoint TenantId bo'yicha filtrlanganini
tekshiring. Bitta izolyatsiya bug'i = boshqa mijoz ma'lumoti oshkor bo'lishi."*

**Natija:** JIDDIY (HIGH) leak topilmadi — avvalgi audit tuzatishlari mustahkam. 21 ta
service to'liq ko'rib chiqildi. 5 ta o'rta/past FK-ishonch bo'shlig'i yopildi:

1. **ProductionService** — retsept yaratish/tahrirlashda `OutputProductId`, `OutputUnitId`,
   bosqich `StageId`/`OutputProductId`/`OutputWarehouseId`, input `ProductId`/`UnitId` endi
   tenant'ga tegishliligi tekshiriladi (eng katta FK yuzasi; `OutputWarehouseId` stock
   yozuviga ulanardi).
2. **KpiService.CreatePlanAsync** — `ShiftId` + `ProductId` tenant tekshiruvi.
3. **KpiService.CreateActualAsync** — `ShiftId` + `ProductId` tenant tekshiruvi.
4. **ProductService** — kategoriya `ParentId` tenant tekshiruvi + o'z-o'ziga parent bo'lish taqiqi.
5. **ExportService** — stock eksportida boshqa tenant ombori nomi subtitle'ga chiqib ketishi yopildi.

**Toza deb tasdiqlangan** (o'zgartirilmadi): TransferService, FinanceService, UserService,
QcService, CounterpartyService, AgentService, WarehouseService, PortalAuthService,
NotificationService, AnalyticsService, AuditService, ImportService, TransferPdfService.

---

## 2. SuperAdmin (control plane roli)

Platforma egasi barcha tenantlarni boshqaradi; oddiy tenant admini faqat o'z tenantida.

- **`User.IsSuperAdmin`** flagi (default `false`) + migration `AddUserIsSuperAdmin`.
- **JWT claim** `isSuperAdmin=true` (faqat superadmin uchun) — `AuthService` login/register.
- **Authorization policy** `"SuperAdmin"` (`RequireClaim("isSuperAdmin","true")`), Program.cs.
- **`BaseController.IsSuperAdmin`** helper.
- **`TenantsController` qayta ishlandi:**
  - Cross-tenant amallar (ro'yxat / yaratish / tahrirlash / o'chirish) → `[Authorize(Policy="SuperAdmin")]`.
  - Modul ko'rish/toggle → o'z tenanti (`settings.modules`) **yoki** SuperAdmin.
  - Avvalgi "tizim tenanti id=1" xaki olib tashlandi.
- **`TenantService`** endi `PlanType`, `SubscriptionStatus`, `CreatedAt`, `UserCount`
  qaytaradi; update plan/status o'zgartirishni va slug noyobligini qo'llab-quvvatlaydi.
- **`TenantProvisioner`** (yangi, umumiy) — bitta joyda to'liq tenant provizatsiyasi:
  Tenant + barcha modullar + "Admin" rol (barcha ruxsatlar) + admin foydalanuvchi.
  Ham SuperAdmin "tenant yaratish", ham self-service register shuni ishlatadi.
- **Seed:** tizim admini (tenant 1) `IsSuperAdmin=true`; `EnsureSuperAdminAsync` mavjud
  bazalarni backfill qiladi.

**Sinov:** admin login → `isSuperAdmin=True`; `GET /api/tenants` → barcha tenantlar
(plan/status/userCount bilan); `POST /api/tenants` → to'liq provizatsiya qilingan tenant
(id=2, Trial); yangi tenant admini login (25 ruxsat, superadmin emas); superadmin bo'lmagan
`GET /api/tenants` → **403**.

---

## 3. Self-service registratsiya

- **`POST /api/auth/register`** (anonim): `{ tenantName, slug, fullName, phone, password }`
  → `TenantProvisioner` orqali yangi tenant (trial obuna) + admin + modullar + rol yaratadi,
  `AuthResponseDto` (token) qaytaradi — yangi admin **darhol tizimga kiradi**.
- **Validatsiya:** slug noyobligi, parol ≥ 6 belgi, majburiy maydonlar.

**Sinov:** register → 25 ruxsat, 9 modul, auto-login (`GET /me` ishladi); dublikat slug → 400;
qisqa parol → 400; registratsiya qilingan admin superadmin emas (`/tenants` → 403).

---

## 4. Frontend (control plane UI)

- **Registratsiya sahifasi** `/auth/register` — kompaniya nomi, avtomat taklif qilinadigan
  slug, admin ism/telefon/parol; POST register, auto-login, dashboard'ga o'tish. Login
  sahifasida "Hisob yaratish" linki.
- **`AuthService.register()`** + `applyAuth()` helper; `isSuperAdmin` computed; User modeli
  `tenantName/isSuperAdmin/permissions/enabledModules` bilan kengaydi.
- **SuperAdmin tenant boshqaruvi** `/superadmin/tenants` (`superAdminGuard` bilan) —
  tenantlar ro'yxati (plan, obuna holati badge, user soni, faollik), yaratish (to'liq
  provizatsiya, admin hisobi bilan), tahrirlash (plan/status/faollik), o'chirish, har
  tenant uchun modul toggle.
- **`SuperAdminService`** + tenant modeli; sidebar'da "Platforma" bo'limi faqat
  superadminlarga ko'rinadi.
- **i18n:** register + superadmin kalitlari uz/ru/en/uz-cyrl.

---

## 5. Migratsiyalar

- `AddUserIsSuperAdmin` — `Users.IsSuperAdmin`.
- (Avvalgi sessiyadan) `AddTenantBillingHooks` — `Tenants.PlanType`, `Tenants.SubscriptionStatus`.

Startup'da avtomat qo'llanadi (`db.Database.Migrate()`), backfill'lar `DataInitializer`da.

---

## 6. Roadmap bo'yicha ATAYIN qoldirilgani (Bosqich 3–5)

Roadmap ketma-ketligiga sodiq qolib, quyidagilar **qurilmadi** (biznes darvozasi hali
ochilmagan — bir necha aktiv mijoz kerak):

- **Bosqich 3:** Telegram bot, Delivery moduli, public marketing sayt.
- **Bosqich 4:** avtomat backup, monitoring/Sentry, CI/CD, performance audit.
- **Bosqich 5:** Billing (Plan/Subscription/Invoice/CLICK/Payme) va AI Advisor.
  *(Poydevor hook'lari — `Tenant.PlanType`/`SubscriptionStatus` va `IAiAdvisorService` stub —
  avvalroq qo'yilgan, shuning uchun keyin qayta-arxitektura shart emas.)*

---

## 7. Ochilishdan oldin tavsiya etiladigan qadamlar

Registratsiya **public** endpoint. Keng jamoatchilikka ochishdan oldin:

1. **Rate limiting** — `POST /api/auth/register` ga (spam tenant yaratishni oldini olish).
   ASP.NET `RateLimiter` middleware (masalan IP bo'yicha soatiga N).
2. **Slug qora ro'yxati** — `admin`, `api`, `www` kabi zaxira slug'lar.
3. **Email/telefon tasdiqlash** (ixtiyoriy MVP'dan keyin) — soxta registratsiyalarni kamaytirish.
4. **CAPTCHA** (ixtiyoriy) — bot registratsiyalariga qarshi.
5. **Trial oqimi** — hozircha `SubscriptionStatus=Trial` faqat belgi; limit enforcement
   Bosqich 5A'da quriladi.

Bular Bosqich 2'ni "ishlaydigan"dan "ishonchli public"ga o'tkazadi — kerak bo'lganda.

---

## Commit tarixi (bu ish)

```
0e29552  Add control plane frontend: register page + SuperAdmin tenant management
a8dcc98  Add self-service registration endpoint
46b32f5  Add SuperAdmin control plane + tenant isolation hardening
```

(Avvalgi bog'liq: `dced2da` Audit Log + poydevor hook'lari.)
