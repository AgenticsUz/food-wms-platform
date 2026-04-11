# WMS API — Loyiha Xulosa va API Hujjati

## 1. Umumiy Ma'lumot

| Parametr | Qiymat |
|---|---|
| Platform | .NET 8 (LTS), ASP.NET Core Web API |
| Database | SQLite (MVP), PostgreSQL-ready |
| Auth | JWT Bearer Token |
| ORM | Entity Framework Core 8 |
| Port | `http://localhost:7040` |
| Swagger | `http://localhost:7040/swagger` |
| Arxitektura | Clean Architecture (Domain → Application → Infrastructure → API) |

---

## 2. Qilingan Ishlar Xulasasi

### Infratuzilma
- .NET 9 → .NET 8 ga downgrade (Windows 10.0.19043 CET muammosi sababli)
- `global.json` — SDK versiyasini 8.0.417 ga pin qilindi
- SQLite bilan to'liq ishlaydigan migration va seed data

### Bug Fixlar
- **TransferItem.TotalPrice** — computed property entity'dan olib tashlandi (EF Core 8 `[NotMapped]` va `Ignore()` ni qabul qilmadi)
- **Transfer → WarehouseStock** — Incoming transfer tasdiqlanganda stock yozilmasligi tuzatildi: `SaveChangesAsync()` har item uchun chaqiriladi, location yo'q bo'lsa avtomatik yaratiladi
- **AnalyticsService** — barcha 11 ta metod SQLite uchun optimizatsiya qilindi: `GroupBy`/`Sum`/`Math.Abs` operatsiyalari DB emas, C# memory da bajariladi
- **WarehouseService.GetStockAsync** — `GroupBy` navigation property bilan SQLite da ishlamasligi tuzatildi

### Swagger
- JWT Bearer autentifikatsiya qo'shildi — Authorize tugmasi (qulf ikonkasi) orqali token kiritish

### Seed Data
- Tenant: `WMS Admin` (slug: `admin`)
- Admin user: phone `998901234567`, password `Admin123456`
- Role: `Admin` — admin userga biriktirilgan
- 9 ta modul — hammasi yoqilgan (IsEnabled = true)

---

## 3. Loyiha Tuzilmasi

```
wms-api/
├── WMS.sln
├── global.json
├── WMS.Domain/           # Entity va Enum'lar
│   ├── Common/BaseEntity.cs
│   ├── Entities/         # 24 ta entity
│   └── Enums/            # 10 ta enum
├── WMS.Application/      # Interface, DTO, Service contract'lar
│   ├── Common/ApiResponse.cs
│   ├── Interfaces/       # 12 ta interface
│   └── DTOs/             # 13 ta DTO fayl (112+ klass)
├── WMS.Infrastructure/   # DB, Service implementation
│   ├── Persistence/
│   │   ├── WmsDbContext.cs
│   │   ├── DataInitializer.cs
│   │   └── Migrations/
│   └── Services/         # 12 ta service
└── WMS.API/              # Controller va middleware
    ├── Controllers/      # 13 ta fayl (20 ta controller)
    ├── Program.cs
    └── appsettings.json
```

---

## 4. API Response Formati

Barcha endpointlar yagona wrapper ishlatadi:

```json
{
  "success": true,
  "data": { ... },
  "message": null
}
```

Xatolik:
```json
{
  "success": false,
  "data": null,
  "message": "Error description"
}
```

---

## 5. Autentifikatsiya

### Login
```
POST /api/auth/login
```
Body:
```json
{
  "phone": "998901234567",
  "password": "Admin123456",
  "tenantSlug": "admin"
}
```
Response:
```json
{
  "token": "eyJhbGciOi...",
  "user": {
    "id": 1,
    "fullName": "Admin",
    "phone": "998901234567",
    "tenantId": 1,
    "tenantName": "WMS Admin",
    "roles": ["Admin"],
    "enabledModules": ["WAREHOUSE_RAW", "PRODUCTION", ...]
  }
}
```

### JWT Token ishlatish
Barcha himoyalangan endpointlarga header qo'shiladi:
```
Authorization: Bearer eyJhbGciOi...
```

JWT claims: `tenantId`, `userId`, `fullName`, `role`

---

## 6. Barcha API Endpointlar

### 6.1 Auth (`/api/auth`)

| Metod | Endpoint | Tavsif | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Tizimga kirish | Yo'q |
| GET | `/api/auth/me` | Joriy foydalanuvchi ma'lumoti | Ha |

---

### 6.2 Tenants (`/api/tenants`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/tenants` | Barcha tenantlar ro'yxati |
| POST | `/api/tenants` | Yangi tenant yaratish |
| PUT | `/api/tenants/{id}` | Tenant yangilash |
| DELETE | `/api/tenants/{id}` | Tenant o'chirish (soft delete) |
| GET | `/api/tenants/{id}/modules` | Tenant modullari |
| PUT | `/api/tenants/{id}/modules` | Modullarni yoqish/o'chirish |

**Request/Response DTOs:**

```
TenantDto            { Id, Name, Slug, IsActive }
CreateTenantDto      { Name, Slug }
UpdateTenantDto      { Name, Slug, IsActive }
TenantModuleDto      { ModuleId, ModuleName, ModuleCode, IsEnabled }
ToggleModuleDto      { ModuleId, IsEnabled }
```

---

### 6.3 Users (`/api/users`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/users` | Foydalanuvchilar ro'yxati (tenantId bo'yicha) |
| POST | `/api/users` | Yangi foydalanuvchi |
| PUT | `/api/users/{id}` | Foydalanuvchi yangilash |
| DELETE | `/api/users/{id}` | Foydalanuvchi o'chirish (soft delete) |
| PUT | `/api/users/{id}/roles` | Rollarni biriktirish |

**DTOs:**

```
UserDto              { Id, FullName, Phone, IsActive, Roles[] }
CreateUserDto        { FullName, Phone, Password }
UpdateUserDto        { FullName, Phone, IsActive, Password? }
AssignRolesDto       { RoleIds[] }
```

---

### 6.4 Roles (`/api/roles`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/roles` | Rollar ro'yxati |
| POST | `/api/roles` | Yangi rol |
| DELETE | `/api/roles/{id}` | Rol o'chirish |

**DTOs:**

```
RoleDto              { Id, Name, Description? }
CreateRoleDto        { Name, Description? }
```

---

### 6.5 Products (`/api/products`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/products` | Mahsulotlar ro'yxati (?page, ?pageSize) |
| POST | `/api/products` | Yangi mahsulot |
| PUT | `/api/products/{id}` | Mahsulot yangilash |
| DELETE | `/api/products/{id}` | Mahsulot o'chirish (soft delete) |

**DTOs:**

```
ProductDto           { Id, Name, CategoryId, CategoryName, UnitId, UnitName,
                       UnitShortName, Type, MinStock, ShelfLifeDays?,
                       Barcode?, CostPrice? }
CreateProductDto     { Name, CategoryId, UnitId, Type, MinStock,
                       ShelfLifeDays?, Barcode?, CostPrice? }
UpdateProductDto     { Name, CategoryId, UnitId, Type, MinStock,
                       ShelfLifeDays?, Barcode?, CostPrice? }
```

**ProductType enum:** `Raw = 1, SemiFinished = 2, Finished = 3`

---

### 6.6 Categories (`/api/categories`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/categories` | Kategoriyalar daraxti |
| POST | `/api/categories` | Yangi kategoriya |

**DTOs:**

```
CategoryDto          { Id, Name, ParentId?, Children[] }
CreateCategoryDto    { Name, ParentId? }
```

---

### 6.7 Units (`/api/units`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/units` | O'lchov birliklari |
| POST | `/api/units` | Yangi o'lchov birligi |

**DTOs:**

```
UnitDto              { Id, Name, ShortName }
CreateUnitDto        { Name, ShortName }
```

---

### 6.8 Counterparties (`/api/counterparties`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/counterparties` | Kontragentlar (?type=Supplier\|Client\|Both) |
| POST | `/api/counterparties` | Yangi kontragent |
| PUT | `/api/counterparties/{id}` | Kontragent yangilash |
| DELETE | `/api/counterparties/{id}` | Kontragent o'chirish (soft delete) |
| GET | `/api/counterparties/{id}/balance` | Balans (qarz) |
| GET | `/api/counterparties/{id}/payments` | To'lov tarixi |

**DTOs:**

```
CounterpartyDto         { Id, Name, Type, Phone?, Address?, Note?,
                          PortalEnabled, PortalPhone? }
CreateCounterpartyDto   { Name, Type, Phone?, Address?, Note?,
                          PortalEnabled, PortalPhone?, PortalPassword? }
UpdateCounterpartyDto   { Name, Type, Phone?, Address?, Note?,
                          PortalEnabled, PortalPhone?, PortalPassword? }
CounterpartyBalanceDto  { CounterpartyId, CounterpartyName, DebtAmount }
```

**CounterpartyType enum:** `Supplier = 1, Client = 2, Both = 3`

---

### 6.9 Warehouses (`/api/warehouses`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/warehouses` | Omborlar ro'yxati |
| POST | `/api/warehouses` | Yangi ombor |
| PUT | `/api/warehouses/{id}` | Ombor yangilash |
| GET | `/api/warehouses/{id}/stock` | Ombor zaxirasi (mahsulot bo'yicha guruh) |
| GET | `/api/warehouses/{id}/stock/detail` | Batafsil zaxira (partiya/joylashuv) |

**DTOs:**

```
WarehouseDto         { Id, Name, Type, Description? }
CreateWarehouseDto   { Name, Type, Description? }
UpdateWarehouseDto   { Name, Type, Description? }
StockDto             { ProductId, ProductName, UnitShortName,
                       TotalQuantity, ReservedQuantity, AvailableQuantity }
StockDetailDto       { ProductId, ProductName, UnitShortName, BatchId,
                       LotNumber, ExpiryDate?, LocationId, LocationName,
                       Quantity, ReservedQuantity }
```

**WarehouseType enum:** `Raw = 1, Finished = 2, General = 3`

---

### 6.10 Locations (`/api/locations`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/locations` | Joylashuvlar (?warehouseId) |
| POST | `/api/locations` | Yangi joylashuv |

**DTOs:**

```
LocationDto          { Id, WarehouseId, WarehouseName, Name, Code? }
CreateLocationDto    { WarehouseId, Name, Code? }
```

---

### 6.11 Batches (`/api/batches`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/batches` | Partiyalar ro'yxati |

**DTOs:**

```
BatchDto             { Id, ProductId, ProductName, LotNumber,
                       ManufacturedDate, ExpiryDate?, InitialQuantity,
                       RemainingQuantity }
```

---

### 6.12 Transfers (`/api/transfers`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/transfers` | Transferlar (?type, ?status, ?from, ?to, ?page, ?pageSize) |
| GET | `/api/transfers/{id}` | Transfer tafsiloti |
| POST | `/api/transfers` | Yangi transfer |
| PUT | `/api/transfers/{id}/confirm` | Tasdiqlash (zaxirani yangilaydi) |
| PUT | `/api/transfers/{id}/reject` | Rad etish |
| DELETE | `/api/transfers/{id}` | Bekor qilish (faqat Pending) |

**DTOs:**

```
TransferDto          { Id, Type, Status, FromWarehouseId?,
                       FromWarehouseName?, ToWarehouseId?,
                       ToWarehouseName?, CounterpartyId?,
                       CounterpartyName?, CreatedByUserId?,
                       CreatedByUserName?, Note?, ConfirmedAt?,
                       CreatedAt, TotalAmount, Items[] }
TransferItemDto      { Id, ProductId, ProductName, UnitShortName,
                       BatchId?, LotNumber?, Quantity, UnitPrice,
                       TotalPrice }
CreateTransferDto    { Type, FromWarehouseId?, ToWarehouseId?,
                       CounterpartyId?, Note?, Items[] }
CreateTransferItemDto { ProductId, BatchId?, LocationId?,
                       Quantity, UnitPrice }
```

**TransferType enum:** `Incoming = 1, Outgoing = 2, Internal = 3, ProductionOutput = 4`
**TransferStatus enum:** `Pending = 1, Confirmed = 2, Rejected = 3, Cancelled = 4`

**Biznes qoidalar:**
- **Incoming tasdiqlanganda:** → Batch yaratiladi, WarehouseStock ga qo'shiladi, qarz yangilanadi
- **Outgoing tasdiqlanganda:** → FEFO qoidasi (birinchi tugaydigan birinchi chiqadi), zaxira kamayadi
- **Internal tasdiqlanganda:** → Manbadan ayiriladi, maqsadga qo'shiladi

---

### 6.13 Production (`/api/production`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/production/stages` | Ishlab chiqarish bosqichlari |
| POST | `/api/production/stages` | Yangi bosqich |
| PUT | `/api/production/stages/{id}` | Bosqich yangilash |
| DELETE | `/api/production/stages/{id}` | Bosqich o'chirish |
| PATCH | `/api/production/stages/reorder` | Bosqichlar tartibini o'zgartirish |
| GET | `/api/production/recipes` | Retseptlar ro'yxati |
| GET | `/api/production/recipes/{id}` | Retsept tafsiloti |
| POST | `/api/production/recipes` | Yangi retsept (bosqichlar + materiallar bilan) |
| PUT | `/api/production/recipes/{id}` | Retsept yangilash |
| DELETE | `/api/production/recipes/{id}` | Retsept o'chirish |
| GET | `/api/production/orders` | Buyurtmalar (?status) |
| GET | `/api/production/orders/{id}` | Buyurtma tafsiloti |
| POST | `/api/production/orders` | Yangi buyurtma |
| PUT | `/api/production/orders/{id}/start` | Buyurtmani boshlash |
| PUT | `/api/production/orders/{id}/stages/{stageId}/execute` | Bosqichni bajarish |
| PUT | `/api/production/orders/{id}/complete` | Buyurtmani yakunlash |

**DTOs:**

```
ProductionStageDto         { Id, Name, OrderNumber, Description? }
CreateProductionStageDto   { Name, OrderNumber, Description? }
UpdateProductionStageDto   { Name, OrderNumber, Description? }
ReorderStageDto            { Id, OrderNumber }

ProductionRecipeDto        { Id, Name, OutputProductId, OutputProductName,
                             OutputQuantity, OutputUnitId, OutputUnitName,
                             IsActive, Stages[] }
RecipeStageDto             { Id, StageId, StageName, OrderNumber,
                             OutputProductId?, OutputProductName?,
                             ExpectedOutputQty?, AllowWarehouseOutput,
                             OutputWarehouseId?, Inputs[] }
RecipeStageItemDto         { Id, ProductId, ProductName, Quantity,
                             UnitId, UnitName }
CreateRecipeDto            { Name, OutputProductId, OutputQuantity,
                             OutputUnitId, Stages[] }
CreateRecipeStageDto       { StageId, OrderNumber, OutputProductId?,
                             ExpectedOutputQty?, AllowWarehouseOutput,
                             OutputWarehouseId?, Inputs[] }
CreateRecipeStageItemDto   { ProductId, Quantity, UnitId }

ProductionOrderDto         { Id, RecipeId, RecipeName, OutputProductName,
                             PlannedQuantity, Status, PlannedStartDate,
                             PlannedEndDate?, AssignedToUserId?,
                             AssignedToUserName?, Note?, CreatedAt,
                             StageExecutions[] }
CreateProductionOrderDto   { RecipeId, PlannedQuantity, PlannedStartDate,
                             PlannedEndDate?, AssignedToUserId?, Note? }
StageExecutionDto          { Id, RecipeStageId, StageName, OrderNumber,
                             PlannedQuantity, ActualQuantity,
                             WasteQuantity, ReworkQuantity,
                             WorkerUserId?, WorkerUserName?,
                             StartTime?, EndTime?, Status, Note? }
ExecuteStageDto            { ActualQuantity, WasteQuantity,
                             ReworkQuantity, WorkerUserId?, Note? }
```

**ProductionOrderStatus enum:** `Draft = 1, InProgress = 2, Completed = 3, Cancelled = 4`
**StageExecutionStatus enum:** `Pending = 1, InProgress = 2, Completed = 3, Skipped = 4`

---

### 6.14 Finance (`/api/finance`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/finance/transactions` | Tranzaksiyalar (?type, ?from, ?to, ?page, ?pageSize) |
| POST | `/api/finance/transactions` | Yangi tranzaksiya |
| GET | `/api/finance/debts` | Barcha qarzlar |
| POST | `/api/finance/payments` | To'lov qayd etish |
| GET | `/api/finance/summary` | Moliya xulosasi |

**DTOs:**

```
TransactionDto       { Id, Type, CounterpartyId?, CounterpartyName?,
                       TransferId?, Amount, Description?, Date,
                       RecordedByUserName }
CreateTransactionDto { Type, CounterpartyId?, TransferId?, Amount,
                       Description?, Date }
DebtDto              { CounterpartyId, CounterpartyName,
                       CounterpartyType, Amount }
CreatePaymentDto     { CounterpartyId, TransferId?, Amount,
                       Method, Note? }
PaymentHistoryDto    { Id, CounterpartyId, CounterpartyName,
                       TransferId?, Amount, Method, PaidAt,
                       Note?, RecordedByUserName }
FinanceSummaryDto    { TotalIncome, TotalExpense, TotalDebt, NetProfit }
```

**TransactionType enum:** `Income = 1, Expense = 2`
**PaymentMethod enum:** `Cash = 1, Bank = 2, Card = 3`

---

### 6.15 KPI — Shifts (`/api/shifts`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/shifts` | Smenalar ro'yxati |
| POST | `/api/shifts` | Yangi smena |

**DTOs:**

```
ShiftDto             { Id, Name, StartTime, EndTime }
CreateShiftDto       { Name, StartTime, EndTime }
```

---

### 6.16 KPI (`/api/kpi`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/kpi/plans` | Smena rejalari (?date) |
| POST | `/api/kpi/plans` | Yangi reja |
| GET | `/api/kpi/actuals` | Haqiqiy natijalar (?date) |
| POST | `/api/kpi/actuals` | Natija qayd etish |
| GET | `/api/kpi/summary` | Reja vs haqiqat xulosasi (?from, ?to) |
| GET | `/api/kpi/efficiency` | Samaradorlik % (?from, ?to) |

**DTOs:**

```
ShiftPlanDto         { Id, ShiftId, ShiftName, ProductId, ProductName,
                       PlannedQuantity, Date }
CreateShiftPlanDto   { ShiftId, ProductId, PlannedQuantity, Date }
ShiftActualDto       { Id, ShiftId, ShiftName, ProductId, ProductName,
                       ActualQuantity, WasteQuantity, Date, Note? }
CreateShiftActualDto { ShiftId, ProductId, ActualQuantity,
                       WasteQuantity, Date, Note? }
KpiSummaryDto        { TotalPlanned, TotalActual, EfficiencyPercent,
                       TotalWaste, WastePercent }
EfficiencyDto        { ShiftName, ProductName, Date, Planned,
                       Actual, EfficiencyPercent }
```

**Formulalar:**
```
Samaradorlik % = (ActualQuantity / PlannedQuantity) * 100
Brak % = (WasteQuantity / ActualQuantity) * 100
```

---

### 6.17 Attendance (`/api/attendance`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| POST | `/api/attendance/checkin` | Ishga kelish qayd etish |
| PUT | `/api/attendance/{id}/checkout` | Ishdan ketish qayd etish |
| GET | `/api/attendance` | Davomat ro'yxati (?userId, ?date) |

**DTOs:**

```
AttendanceLogDto     { Id, UserId, UserName, ShiftId, ShiftName,
                       CheckIn, CheckOut?, Method, DeviceId? }
CheckInDto           { UserId, ShiftId, Method, DeviceId? }
```

**AttendanceMethod enum:** `PIN = 1, FaceID = 2, Manual = 3`

---

### 6.18 Quality Control (`/api/qc`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/qc/parameters` | QC parametrlari |
| POST | `/api/qc/parameters` | Yangi parametr |
| GET | `/api/qc/checks` | Tekshiruvlar (?transferId, ?stageExecutionId) |
| POST | `/api/qc/checks` | Tekshiruv qayd etish |

**DTOs:**

```
QcParameterDto       { Id, Name, Unit?, MinValue?, MaxValue?, ValueType }
CreateQcParameterDto { Name, Unit?, MinValue?, MaxValue?, ValueType }
QcCheckDto           { Id, StageExecutionId?, TransferId?, ParameterId,
                       ParameterName, Value, IsPassed,
                       CheckedByUserName, CheckedAt, Note? }
CreateQcCheckDto     { StageExecutionId?, TransferId?, ParameterId,
                       Value, IsPassed, Note? }
```

**QcParameterType enum:** `Numeric = 1, Text = 2, Boolean = 3`

---

### 6.19 Analytics (`/api/analytics`)

| Metod | Endpoint | Tavsif |
|---|---|---|
| GET | `/api/analytics/summary` | Dashboard xulosa kartochkalari |
| GET | `/api/analytics/production/plan-vs-actual` | Reja vs haqiqat (?days=7) |
| GET | `/api/analytics/production/waste-by-stage` | Brak % bosqich bo'yicha (?days=30) |
| GET | `/api/analytics/transfers/daily` | Kunlik transferlar (?days=7) |
| GET | `/api/analytics/warehouse/stock-levels` | Zaxira darajalari (?warehouseId) |
| GET | `/api/analytics/warehouse/stock-history` | Zaxira harakati tarixi (?days=30) |
| GET | `/api/analytics/finance/income-expense` | Daromad vs xarajat (?days=30) |
| GET | `/api/analytics/finance/top-debtors` | Eng katta qarzdorlar (?top=10) |
| GET | `/api/analytics/kpi/shift-efficiency` | Smena samaradorligi (?days=7) |
| GET | `/api/analytics/kpi/attendance-heatmap` | Davomat issiqlik xaritasi (?days=30) |
| GET | `/api/analytics/products/distribution` | Mahsulot taqsimoti |

**DTOs:**

```
DashboardSummaryDto     { TotalStockKg, ActiveProductionOrders,
                          PendingTransfers, MonthlyRevenue,
                          RevenueChangePercent, LowStockProductCount,
                          OverallEfficiencyPercent, TotalDebt }
PlanVsActualDto         { Date, ProductName, Planned, Actual,
                          EfficiencyPercent }
WasteByStageDto         { StageName, TotalActual, TotalWaste,
                          WastePercent }
DailyTransferDto        { Date, IncomingTotal, OutgoingTotal,
                          IncomingCount, OutgoingCount }
StockLevelDto           { ProductName, UnitShortName, CurrentStock,
                          MinStock, IsLow }
StockHistoryDto         { Date, ProductName, Quantity, MovementType }
IncomeExpenseDto        { Date, Income, Expense, Net }
TopDebtorDto            { CounterpartyName, Type, DebtAmount }
ShiftEfficiencyDto      { ShiftName, Date, PlannedQty, ActualQty,
                          EfficiencyPercent }
AttendanceHeatmapDto    { WorkerName, Date, HoursWorked, WasPresent }
ProductDistributionDto  { ProductName, Type, TotalStock, Percentage }
```

---

### 6.20 Counterparty Portal (`/api/portal`)

> Portal — kontragentlar (yetkazib beruvchi/mijoz) uchun alohida tizim.
> Alohida JWT token, faqat o'qish rejimi (Phase 1).

| Metod | Endpoint | Tavsif | Auth |
|---|---|---|---|
| POST | `/api/portal/login` | Portal tizimiga kirish | Yo'q |
| GET | `/api/portal/me` | Joriy kontragent ma'lumoti | Ha |
| GET | `/api/portal/transfers` | O'z transferlari (?page, ?pageSize) | Ha |
| GET | `/api/portal/transfers/{id}` | Transfer tafsiloti | Ha |
| GET | `/api/portal/finance` | Balans va qarz | Ha |
| GET | `/api/portal/payments` | To'lov tarixi | Ha |

**DTOs:**

```
PortalLoginDto          { Phone, Password }
PortalAuthResponseDto   { Token, Counterparty }
PortalCounterpartyDto   { Id, Name, Type, TenantId }
PortalFinanceDto        { Balance, TotalDebt }
```

---

## 7. Entity Schema (Database)

### Asosiy Entity'lar (24 ta)

| Entity | Jadval | Tavsif |
|---|---|---|
| Tenant | Tenants | Tashkilot (zavod) |
| User | Users | Foydalanuvchi |
| Role | Roles | Rol |
| UserRole | UserRoles | Foydalanuvchi-rol bog'lanishi |
| Module | Modules | Tizim moduli (seeded, 9 ta) |
| TenantModule | TenantModules | Tenant modul sozlamasi |
| Category | Categories | Mahsulot kategoriyasi (daraxt) |
| Unit | Units | O'lchov birligi |
| Product | Products | Mahsulot |
| Counterparty | Counterparties | Kontragent (yetkazib beruvchi/mijoz) |
| Warehouse | Warehouses | Ombor |
| Location | Locations | Ombor ichidagi joy |
| Batch | Batches | Partiya/lot |
| WarehouseStock | WarehouseStocks | Joriy zaxira (joy/partiya bo'yicha) |
| Transfer | Transfers | Transfer (kirim/chiqim/ichki) |
| TransferItem | TransferItems | Transfer tarkibi |
| ProductionStage | ProductionStages | Ishlab chiqarish bosqichi |
| ProductionRecipe | ProductionRecipes | Retsept |
| RecipeStage | RecipeStages | Retsept bosqichi |
| RecipeStageItem | RecipeStageItems | Bosqich uchun kerakli materiallar |
| ProductionOrder | ProductionOrders | Ishlab chiqarish buyurtmasi |
| StageExecution | StageExecutions | Bosqich bajarilishi |
| QcParameter | QcParameters | Sifat nazorati parametri |
| QcCheck | QcChecks | Sifat tekshiruvi |
| Transaction | Transactions | Moliyaviy tranzaksiya |
| Debt | Debts | Qarz holati |
| PaymentHistory | PaymentHistories | To'lov tarixi |
| Shift | Shifts | Smena |
| ShiftPlan | ShiftPlans | Smena rejasi |
| ShiftActual | ShiftActuals | Smena haqiqiy natijasi |
| AttendanceLog | AttendanceLogs | Davomat qaydnomasi |

### Modullar (seeded)

| ID | Code | Nomi |
|---|---|---|
| 1 | WAREHOUSE_RAW | Xom ashyo ombori |
| 2 | PRODUCTION | Ishlab chiqarish |
| 3 | WAREHOUSE_FINISHED | Tayyor mahsulot ombori |
| 4 | TRANSFERS | Transfer tizimi |
| 5 | FINANCE | Moliya |
| 6 | KPI | KPI va smenalar |
| 7 | SUPPLIERS | Yetkazib beruvchilar |
| 8 | CLIENTS | Mijozlar |
| 9 | QUALITY | Sifat nazorati |

---

## 8. Jami Statistika

| Metrika | Qiymat |
|---|---|
| Controller fayllar | 13 |
| Controller klasslari | 20 |
| Endpointlar | 78 |
| DTO klasslari | 112+ |
| Entity klasslari | 24 |
| Enum'lar | 10 |
| Service'lar | 12 |
| Database jadvallar | 31 (+ __EFMigrationsHistory) |
