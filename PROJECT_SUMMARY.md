# WMS Platform - Loyiha Xulosasi

> Yaratilgan sana: 2026-04-10
> Loyiha turi: Oziq-ovqat ishlab chiqaruvchilar uchun modular SaaS platforma

---

## 1. UMUMIY MA'LUMOT

WMS (Warehouse Management System) — ko'p kirayechi (multi-tenant) arxitekturada qurilgan ombor boshqaruv tizimi. Muzqaymoq, sut mahsulotlari, qandolatchilik va shokolad ishlab chiqaruvchi zavodlar uchun mo'ljallangan.

**Texnologiyalar:**

| Qatlam | Texnologiya |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API (port: 7040) |
| ORM | Entity Framework Core 8 |
| Ma'lumotlar bazasi | SQLite (MVP) |
| Autentifikatsiya | JWT Bearer Tokens |
| Frontend | Angular 21 (standalone, signals, zoneless) (port: 7050) |
| UI kutubxona | PrimeNG 21.1.5 |
| CSS | Tailwind CSS v4 |
| Grafiklar | ApexCharts + ng-apexcharts |
| Ikonalar | PrimeIcons |
| Tillar | Transloco (uz, ru, en) |

---

## 2. LOYIHA TUZILMASI

### 2.1 Umumiy papka tuzilmasi

```
wms/
├── CLAUDE.md                    # Loyiha dokumentatsiyasi
├── PROJECT_SUMMARY.md           # Shu fayl
├── wms-api/                     # Backend (.NET 8)
│   ├── WMS.sln
│   ├── WMS.Domain/              # Entity va Enum'lar
│   ├── WMS.Application/         # Servis interfeyslari va DTO'lar
│   ├── WMS.Infrastructure/      # Servis implementatsiyalari va DB
│   └── WMS.API/                 # Controller'lar va Program.cs
└── wms-ui/                      # Frontend (Angular 21)
    ├── src/app/core/            # Servislar, guard'lar, interceptor'lar
    ├── src/app/layout/          # Shell, sidebar, header
    ├── src/app/modules/         # Funksional modullar
    ├── src/app/shared/          # Qayta foydalaniladigan komponentlar
    └── src/assets/i18n/         # Tarjima fayllari
```

### 2.2 Backend tuzilmasi (104 fayl)

```
wms-api/
├── WMS.Domain/
│   ├── Common/
│   │   └── BaseEntity.cs              # Id, CreatedAt, UpdatedAt, IsDeleted
│   ├── Entities/
│   │   ├── Tenant.cs                  # Kirayechi (zavod)
│   │   ├── User.cs                    # Foydalanuvchi
│   │   ├── Role.cs                    # Rol
│   │   ├── UserRole.cs                # Foydalanuvchi-Rol bog'lanishi
│   │   ├── Module.cs                  # Tizim modullari (9 ta seed)
│   │   ├── TenantModule.cs            # Kirayechiga biriktirilgan modullar
│   │   ├── Category.cs                # Mahsulot kategoriyasi (daraxt)
│   │   ├── Unit.cs                    # O'lchov birligi (kg, litr)
│   │   ├── Product.cs                 # Mahsulot
│   │   ├── Counterparty.cs            # Yetkazib beruvchi / Mijoz
│   │   ├── Warehouse.cs               # Ombor
│   │   ├── Location.cs                # Ombor ichidagi joy (tokcha/muzlatgich)
│   │   ├── Batch.cs                   # Partiya/LOT (kuzatuvchanlik)
│   │   ├── WarehouseStock.cs          # Joriy zaxira (mahsulot/partiya/joy)
│   │   ├── Transfer.cs                # Transfer buyurtmasi
│   │   ├── TransferItem.cs            # Transfer qatori
│   │   ├── ProductionStage.cs         # Ishlab chiqarish bosqichi
│   │   ├── ProductionRecipe.cs        # Retsept
│   │   ├── RecipeStage.cs             # Retsept bosqichi
│   │   ├── RecipeStageItem.cs         # Bosqich kirish materiallari
│   │   ├── ProductionOrder.cs         # Ishlab chiqarish buyurtmasi
│   │   ├── StageExecution.cs          # Bosqich bajarilishi
│   │   ├── QcParameter.cs             # Sifat nazorati parametri
│   │   ├── QcCheck.cs                 # Sifat tekshiruvi
│   │   ├── Transaction.cs             # Moliyaviy tranzaksiya
│   │   ├── Debt.cs                    # Qarz holati
│   │   ├── PaymentHistory.cs          # To'lov tarixi
│   │   ├── Shift.cs                   # Smena
│   │   ├── ShiftPlan.cs               # Smena rejasi
│   │   ├── ShiftActual.cs             # Smena haqiqiy natijasi
│   │   └── AttendanceLog.cs           # Davomat
│   └── Enums/
│       ├── ProductType.cs             # Raw, SemiFinished, Finished
│       ├── CounterpartyType.cs        # Supplier, Client, Both
│       ├── WarehouseType.cs           # Raw, Finished, General
│       ├── TransferType.cs            # Incoming, Outgoing, Internal, ProductionOutput
│       ├── TransferStatus.cs          # Pending, Confirmed, Rejected, Cancelled
│       ├── ProductionOrderStatus.cs   # Draft, InProgress, Completed, Cancelled
│       ├── StageExecutionStatus.cs    # Pending, InProgress, Completed, Skipped
│       ├── QcParameterType.cs         # Numeric, Text, Boolean
│       ├── TransactionType.cs         # Income, Expense
│       ├── PaymentMethod.cs           # Cash, Bank, Card
│       └── AttendanceMethod.cs        # PIN, FaceID, Manual
│
├── WMS.Application/
│   ├── Common/
│   │   ├── ApiResponse.cs             # Standart javob o'rami
│   │   └── PaginatedList.cs           # Sahifalangan ro'yxat
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── ITenantService.cs
│   │   ├── IUserService.cs
│   │   ├── IProductService.cs
│   │   ├── ICounterpartyService.cs
│   │   ├── IWarehouseService.cs
│   │   ├── ITransferService.cs
│   │   ├── IProductionService.cs
│   │   ├── IFinanceService.cs
│   │   ├── IKpiService.cs
│   │   ├── IQcService.cs
│   │   ├── IPortalAuthService.cs
│   │   └── IAnalyticsService.cs
│   └── DTOs/
│       ├── Auth/                      # LoginDto, AuthResponseDto
│       ├── Products/                  # ProductDto, CategoryDto, UnitDto
│       ├── Warehouse/                 # WarehouseDto, StockDto, BatchDto
│       ├── Transfers/                 # TransferDto, TransferItemDto
│       ├── Production/               # RecipeDto, OrderDto, StageDto
│       ├── Finance/                   # TransactionDto, DebtDto, PaymentDto
│       ├── Kpi/                       # ShiftDto, PlanDto, ActualDto
│       ├── Counterparties/            # CounterpartyDto
│       └── Analytics/                 # DashboardSummaryDto va boshqalar
│
├── WMS.Infrastructure/
│   ├── Persistence/
│   │   ├── WmsDbContext.cs            # 27 ta DbSet, soft-delete filter
│   │   ├── DataInitializer.cs         # Demo ma'lumotlar
│   │   └── Migrations/               # EF Core migratsiyalar
│   └── Services/
│       ├── AuthService.cs
│       ├── TenantService.cs
│       ├── UserService.cs
│       ├── ProductService.cs
│       ├── CounterpartyService.cs
│       ├── WarehouseService.cs
│       ├── TransferService.cs
│       ├── ProductionService.cs
│       ├── FinanceService.cs
│       ├── KpiService.cs
│       ├── QcService.cs
│       ├── PortalAuthService.cs
│       └── AnalyticsService.cs
│
└── WMS.API/
    ├── Controllers/
    │   ├── BaseController.cs          # TenantId, UserId JWT'dan
    │   ├── AuthController.cs
    │   ├── TenantsController.cs
    │   ├── UsersController.cs
    │   ├── ProductsController.cs
    │   ├── CounterpartiesController.cs
    │   ├── WarehousesController.cs
    │   ├── TransfersController.cs
    │   ├── ProductionController.cs
    │   ├── FinanceController.cs
    │   ├── KpiController.cs
    │   ├── QcController.cs
    │   ├── AnalyticsController.cs
    │   └── PortalController.cs
    └── Program.cs                     # DI, JWT, CORS, Swagger
```

### 2.3 Frontend tuzilmasi (48 komponent)

```
wms-ui/src/app/
├── core/
│   ├── services/
│   │   ├── auth.service.ts            # JWT login, token boshqaruvi
│   │   ├── api.service.ts             # HTTP wrapper (get/post/put/delete)
│   │   ├── tenant.service.ts          # Modul boshqaruvi
│   │   ├── product.service.ts         # Mahsulot CRUD
│   │   ├── warehouse.service.ts       # Ombor operatsiyalari
│   │   ├── transfer.service.ts        # Transfer operatsiyalari
│   │   ├── production.service.ts      # Ishlab chiqarish
│   │   ├── counterparty.service.ts    # Kontragentlar
│   │   ├── finance.service.ts         # Moliya
│   │   ├── kpi.service.ts             # KPI va smenalar
│   │   ├── analytics.service.ts       # Dashboard analitikasi
│   │   ├── portal.service.ts          # Portal servisi
│   │   ├── theme.service.ts           # Mavzu (dark/light)
│   │   └── transloco-loader.ts        # Til yuklash
│   ├── guards/
│   │   ├── auth.guard.ts              # Autentifikatsiya himoyasi
│   │   ├── module.guard.ts            # Modul ruxsati
│   │   └── portal.guard.ts            # Portal himoyasi
│   ├── interceptors/
│   │   ├── auth.interceptor.ts        # Token qo'shish
│   │   └── error.interceptor.ts       # Xato ushlash
│   ├── models/
│   │   ├── auth.model.ts
│   │   ├── product.model.ts
│   │   ├── warehouse.model.ts
│   │   ├── transfer.model.ts
│   │   ├── production.model.ts
│   │   ├── counterparty.model.ts
│   │   ├── finance.model.ts
│   │   ├── kpi.model.ts
│   │   ├── analytics.model.ts
│   │   └── portal.model.ts
│   └── config/
│       └── apex-defaults.ts           # ApexCharts global sozlamalar
│
├── layout/
│   ├── shell/shell.component.ts       # Asosiy layout (sidebar + content)
│   ├── header/header.component.ts     # Yuqori panel
│   ├── sidebar/sidebar.component.ts   # Yon panel (dinamik nav)
│   └── portal-layout.component.ts     # Portal layout (alohida)
│
├── shared/
│   ├── components/
│   │   ├── page-header.component.ts   # Sahifa sarlavhasi + breadcrumb
│   │   └── status-badge.component.ts  # Holat belgisi (rangli)
│   └── services/
│       └── notification.service.ts    # Toast xabarnomalar
│
├── modules/
│   ├── auth/
│   │   └── login.component.ts         # Kirish sahifasi
│   │
│   ├── dashboard/
│   │   └── dashboard.component.ts     # Bosh sahifa (kartalar + grafiklar)
│   │
│   ├── warehouse/                     # 5 sahifa
│   │   ├── stock-overview.component.ts
│   │   ├── warehouse-list.component.ts
│   │   ├── movements.component.ts
│   │   ├── locations.component.ts
│   │   └── batches.component.ts
│   │
│   ├── production/                    # 7 sahifa
│   │   ├── stages.component.ts
│   │   ├── recipe-list.component.ts
│   │   ├── recipe-create.component.ts
│   │   ├── recipe-detail.component.ts
│   │   ├── order-list.component.ts
│   │   ├── order-create.component.ts
│   │   └── order-detail.component.ts
│   │
│   ├── transfers/                     # 3 sahifa
│   │   ├── transfer-list.component.ts
│   │   ├── transfer-create.component.ts
│   │   └── transfer-detail.component.ts
│   │
│   ├── finance/                       # 4 sahifa
│   │   ├── transactions.component.ts
│   │   ├── debts.component.ts
│   │   ├── payments.component.ts
│   │   └── finance-summary.component.ts
│   │
│   ├── kpi/                           # 5 sahifa
│   │   ├── dashboard.component.ts
│   │   ├── shifts.component.ts
│   │   ├── plans.component.ts
│   │   ├── actuals.component.ts
│   │   └── attendance.component.ts
│   │
│   ├── counterparties/                # 3 sahifa
│   │   ├── supplier-list.component.ts
│   │   ├── client-list.component.ts
│   │   └── detail.component.ts
│   │
│   ├── products/                      # 3 sahifa
│   │   ├── product-list.component.ts
│   │   ├── categories.component.ts
│   │   └── units.component.ts
│   │
│   ├── settings/                      # 5 sahifa
│   │   ├── users.component.ts
│   │   ├── roles.component.ts
│   │   ├── modules.component.ts
│   │   ├── qc-parameters.component.ts
│   │   └── profile.component.ts
│   │
│   └── portal/                        # 5 sahifa
│       ├── portal-login.component.ts
│       ├── portal-layout.component.ts
│       ├── portal-dashboard.component.ts
│       ├── portal-transfers.component.ts
│       └── portal-finance.component.ts
│
└── assets/i18n/
    ├── uz.json                        # O'zbek tili
    ├── ru.json                        # Rus tili
    └── en.json                        # Ingliz tili
```

---

## 3. MA'LUMOTLAR BAZASI SXEMASI (27 jadval)

```
┌──────────────────────────────────────────────────────────┐
│                    TENANT & AUTH                          │
│  Tenant ─┬── User ──── UserRole ──── Role                │
│           └── TenantModule ──── Module (9 ta seed)       │
├──────────────────────────────────────────────────────────┤
│                     PRODUCTS                             │
│  Category (daraxt) ──── Product ──── Unit                │
├──────────────────────────────────────────────────────────┤
│                   COUNTERPARTIES                         │
│  Counterparty (Supplier / Client / Both + Portal)        │
├──────────────────────────────────────────────────────────┤
│                    WAREHOUSE                             │
│  Warehouse ──── Location                                 │
│  Batch ──── WarehouseStock (mahsulot/partiya/joy)        │
│  Transfer ──── TransferItem                              │
├──────────────────────────────────────────────────────────┤
│                   PRODUCTION                             │
│  ProductionStage                                         │
│  ProductionRecipe ──┬── RecipeStage ──── RecipeStageItem │
│  ProductionOrder ───┴── StageExecution                   │
├──────────────────────────────────────────────────────────┤
│                 QUALITY CONTROL                          │
│  QcParameter ──── QcCheck (Transfer yoki Stage uchun)    │
├──────────────────────────────────────────────────────────┤
│                    FINANCE                               │
│  Transaction ──── Debt ──── PaymentHistory               │
├──────────────────────────────────────────────────────────┤
│                   KPI & SHIFTS                           │
│  Shift ──┬── ShiftPlan                                   │
│          ├── ShiftActual                                 │
│          └── AttendanceLog                               │
└──────────────────────────────────────────────────────────┘
```

---

## 4. API ENDPOINT'LARI (14 controller, 70+ endpoint)

| Controller | Endpoint'lar | Holat |
|---|---|---|
| **Auth** | POST /login, GET /me | Tayyor |
| **Tenants** | GET/POST/PUT/DELETE tenants, GET/PUT modules | Tayyor |
| **Users** | GET/POST/PUT/DELETE users, PUT roles | Tayyor |
| **Products** | GET/POST/PUT/DELETE products, categories, units | Tayyor |
| **Counterparties** | GET/POST/PUT/DELETE, balance, payments | Tayyor |
| **Warehouses** | GET/POST/PUT, stock, stock/detail, locations | Tayyor |
| **Transfers** | GET/POST, confirm, reject, cancel | Tayyor |
| **Production** | stages CRUD, recipes CRUD, orders + execute + complete | Tayyor |
| **Finance** | transactions, debts, payments, summary | Tayyor |
| **KPI** | shifts, plans, actuals, efficiency, attendance | Tayyor |
| **QC** | parameters, checks | Tayyor |
| **Analytics** | summary, plan-vs-actual, daily-transfers, stock-levels va h.k. | Tayyor |
| **Portal** | login, me, transfers, finance, payments | Tayyor |

---

## 5. BAJARILGAN ISHLAR XULOSASI

### Phase 1 — Backend Asos (Tayyor)
- [x] Solution va proyektlar yaratildi (.NET 8 Clean Architecture)
- [x] 30+ entity WMS.Domain'da aniqlandi
- [x] WmsDbContext 27 ta DbSet bilan sozlandi
- [x] Global soft-delete filter qo'shildi
- [x] Migratsiya yaratildi va SQLite sxemasi tasdiqlandi
- [x] JWT autentifikatsiya (AuthService) implementatsiya qilindi
- [x] AuthController yaratildi
- [x] Swagger orqali login test qilindi

### Phase 2 — Asosiy Backend (Tayyor)
- [x] ProductService + ProductsController (mahsulot, kategoriya, birlik)
- [x] CounterpartyService + CounterpartiesController
- [x] WarehouseService + WarehousesController (zaxira boshqaruvi)
- [x] TransferService + TransfersController (zaxira yangilash logikasi bilan)
- [x] ProductionService + ProductionController (retsept, buyurtma, bosqich)
- [x] FinanceService + FinanceController (tranzaksiya, qarz, to'lov)
- [x] KpiService + KpiController (smena, reja, haqiqiy)
- [x] QcService + QcController (sifat nazorati)
- [x] PortalAuthService + PortalController (kontragent portali)
- [x] AnalyticsService + AnalyticsController (dashboard analitikasi)
- [x] Barcha servislar DI'ga ro'yxatdan o'tkazildi

### Phase 3 — Frontend Asos (Tayyor)
- [x] Angular 21 ilovasi yaratildi (standalone, signals, zoneless)
- [x] PrimeNG 21, Tailwind CSS v4, ApexCharts o'rnatildi
- [x] AuthService, login sahifasi, auth guard implementatsiya qilindi
- [x] ShellComponent qurildi (sidebar + header layout)
- [x] Dinamik sidebar modul-asosida yaratildi
- [x] ApiService bazaviy klass implementatsiya qilindi
- [x] Error interceptor qo'shildi

### Phase 4 — Funksional Sahifalar (Tayyor)
- [x] AnalyticsService (Angular) + TypeScript interfeyslari
- [x] Dashboard sahifasi (xulosa kartalar + ApexCharts grafiklar)
- [x] Mahsulotlar moduli (ro'yxat, CRUD, kategoriyalar, birliklar)
- [x] Kontragentlar moduli (yetkazib beruvchilar, mijozlar, detail)
- [x] Ombor moduli (zaxira ko'rinishi, harakatlar, partiyalar, joylar)
- [x] Transferlar moduli (ro'yxat, yaratish, tasdiqlash/rad etish)
- [x] Ishlab chiqarish moduli (bosqichlar, retseptlar, buyurtmalar)
- [x] Moliya moduli (tranzaksiyalar, qarzlar, to'lovlar, xulosa)
- [x] KPI moduli (smenalar, rejalar, haqiqiy, davomat)
- [x] Sozlamalar moduli (foydalanuvchilar, rollar, modullar, QC, profil)
- [x] Portal moduli (login, dashboard, transferlar, moliya)

### Phase 5 — Pardozlash (Tayyor)
- [x] Transloco i18n (uz + ru + en)
- [x] Dark/light mode almashtirish
- [x] Yuklash holatlari va xato ushlash
- [x] Mobil moslashuvchanlik

---

## 6. ASOSIY BIZNES LOGIKASI

### Transfer -> Zaxira yangilanishi
- **Tasdiqlanganda**: Incoming = zaxiraga qo'shish, Outgoing = zaxiradan ayirish, Internal = bir ombordan ikkinchisiga
- **FEFO qoidasi**: muddati birinchi tugaydigan partiyadan olish

### Ishlab chiqarish -> Zaxira
- Bosqich bajarilganda: kirishlar zaxiradan ayiriladi
- Oxirgi bosqich tugaganda: tayyor mahsulot omboriga qo'shiladi, yangi Batch yaratiladi

### Moliya -> Qarz
- Chiqish transferi tasdiqlanganda: mijoz qarzi oshadi
- Kirish transferi tasdiqlanganda: bizning qarzimiz oshadi
- To'lov qayd etilganda: qarz muvofiq kamayadi

### KPI samaradorlik
- `Samaradorlik % = (Haqiqiy / Rejalashtirilgan) * 100`
- `Brak % = (Brak / Haqiqiy) * 100`

---

## 7. STATISTIKA

| Ko'rsatkich | Qiymat |
|---|---|
| Backend fayllar | ~104 ta |
| Frontend komponentlar | ~48 ta |
| Frontend servislar | 14 ta |
| API controller'lar | 14 ta |
| Servis implementatsiyalari | 13 ta |
| DB jadvallar | 27 ta |
| Enum'lar | 11 ta |
| Tarjima tillari | 3 ta (uz, ru, en) |
| Umumiy kod satrlari | ~18,000+ |

---

## 8. ISHGA TUSHIRISH

### Development
```bash
# Backend
cd wms-api
dotnet run --project WMS.API    # http://localhost:7040

# Frontend
cd wms-ui
ng serve                         # http://localhost:7050
```

### Production (Ubuntu)
```bash
# Backend -> systemd xizmati sifatida
dotnet publish WMS.API -c Release -o /var/www/wms-api

# Frontend -> Nginx static hosting
ng build --configuration production
# dist/ -> /var/www/wms-ui

# Nginx: / -> frontend, /api -> backend proxy
```
