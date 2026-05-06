# CLAUDE.md — WMS Platform: Complete Project Documentation

> This document is the single source of truth for the WMS platform.
> The agent must follow this document to build the entire project from scratch.
> Do not skip any section. Implement everything in the order defined here.

---

## 1. PROJECT OVERVIEW

### Purpose
A modular SaaS platform for food manufacturers (ice cream, dairy, confectionery, chocolate).
Each tenant (factory) can enable or disable modules based on their needs.

### Key Principles
- Multi-tenant architecture (TenantId on every table)
- Modular: each feature is a toggleable module
- Role-based access control
- Built for small to large factories
- SQLite for MVP, PostgreSQL-ready (only connection string changes)

---

## 2. TECH STACK

| Layer | Technology |
|---|---|
| Backend | .NET 8 LTS, ASP.NET Core Web API (port: 7040) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (MVP) |
| Auth | JWT Bearer Tokens |
| Frontend | Angular 21 (standalone, signals, zoneless) (port: 7050) |
| UI Library | PrimeNG 21.1.5 (Angular 21 compatible) |
| CSS | Tailwind CSS v4 |
| Charts | ApexCharts + ng-apexcharts |
| Icons | PrimeIcons (built-in PrimeNG) |
| Notifications | PrimeNG Toast + ConfirmDialog (no SweetAlert needed) |
| HTTP Client | Angular HttpClient with interceptors |
| State | Angular Signals (no NgRx) |
| IDE | JetBrains Rider (backend), WebStorm (frontend) |

---

## 3. REPOSITORY STRUCTURE

```
wms-platform/
├── backend/
│   ├── WMS.sln
│   ├── WMS.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Common/
│   ├── WMS.Application/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── DTOs/
│   ├── WMS.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── WmsDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Configurations/   # EF Fluent API configs
│   │   └── Repositories/
│   └── WMS.API/
│       ├── Controllers/
│       ├── Extensions/
│       ├── Middleware/
│       └── Program.cs
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── auth/
│   │   │   │   ├── guards/
│   │   │   │   ├── interceptors/
│   │   │   │   └── services/
│   │   │   ├── shared/
│   │   │   │   ├── components/
│   │   │   │   ├── pipes/
│   │   │   │   └── directives/
│   │   │   ├── layout/
│   │   │   │   ├── shell/
│   │   │   │   ├── sidebar/
│   │   │   │   └── header/
│   │   │   └── modules/
│   │   │       ├── auth/
│   │   │       ├── dashboard/
│   │   │       ├── warehouse/
│   │   │       ├── production/
│   │   │       ├── transfers/
│   │   │       ├── finance/
│   │   │       ├── kpi/
│   │   │       ├── counterparties/
│   │   │       ├── products/
│   │   │       ├── settings/
│   │   │       └── portal/            # Counterparty self-service portal
│   │   ├── environments/
│   │   └── assets/
│   ├── angular.json
│   ├── tailwind.config.ts
│   └── package.json
├── CLAUDE.md
└── README.md
```

---

## 4. DATABASE SCHEMA

### 4.1 Base Entity

```csharp
// WMS.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false; // soft delete
}
```

---

### 4.2 Tenant & Auth

```csharp
// Tenant.cs
public class Tenant : BaseEntity
{
    public string Name { get; set; }
    public string Slug { get; set; }           // unique, url-safe
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; }
    public ICollection<TenantModule> TenantModules { get; set; }
}

// User.cs
public class User : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }          // used as login username
    public string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; }
}

// Role.cs
public class Role : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; }
}

// UserRole.cs (many-to-many)
public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; }
}

// Module.cs (system-level, seeded)
public class Module : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }           // e.g. "WAREHOUSE_RAW"
    public string? Description { get; set; }
    public int OrderNumber { get; set; }
    public ICollection<TenantModule> TenantModules { get; set; }
}

// TenantModule.cs
public class TenantModule : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
    public int ModuleId { get; set; }
    public Module Module { get; set; }
    public bool IsEnabled { get; set; } = true;
}
```

**Module seed data:**
| Code | Name |
|---|---|
| `WAREHOUSE_RAW` | Raw Material Warehouse |
| `PRODUCTION` | Production |
| `WAREHOUSE_FINISHED` | Finished Goods Warehouse |
| `TRANSFERS` | Transfer System |
| `FINANCE` | Finance |
| `KPI` | KPI & Shifts |
| `SUPPLIERS` | Suppliers |
| `CLIENTS` | Clients |
| `QUALITY` | Quality Control |

---

### 4.3 Products

```csharp
// Category.cs
public class Category : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }         // tree structure
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; }
}

// Unit.cs
public class Unit : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }           // "Kilogram"
    public string ShortName { get; set; }      // "kg"
}

// Product.cs
public class Product : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; }
    public ProductType Type { get; set; }      // Raw, SemiFinished, Finished
    public decimal MinStock { get; set; } = 0;
    public int? ShelfLifeDays { get; set; }    // null = no expiry
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}

// Enums/ProductType.cs
public enum ProductType { Raw = 1, SemiFinished = 2, Finished = 3 }
```

---

### 4.4 Counterparties

```csharp
// Counterparty.cs
public class Counterparty : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }
    public CounterpartyType Type { get; set; } // Supplier, Client, Both
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    // Portal access (optional — for supplier/client self-service)
    public string? PortalPhone { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool PortalEnabled { get; set; } = false;
}

public enum CounterpartyType { Supplier = 1, Client = 2, Both = 3 }
```

---

### 4.5 Warehouse

```csharp
// Warehouse.cs
public class Warehouse : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }
    public WarehouseType Type { get; set; }    // Raw, Finished, General
    public string? Description { get; set; }
    public ICollection<Location> Locations { get; set; }
}

public enum WarehouseType { Raw = 1, Finished = 2, General = 3 }

// Location.cs (bin/shelf inside warehouse)
public class Location : BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public string Name { get; set; }           // "Freezer A-1"
    public string? Code { get; set; }          // "A1"
}

// Batch.cs (lot/batch — traceability)
public class Batch : BaseEntity
{
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public string LotNumber { get; set; }      // auto-generated or manual
    public DateTime ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
}

// WarehouseStock.cs (current stock per location per batch)
public class WarehouseStock : BaseEntity
{
    public int TenantId { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int BatchId { get; set; }
    public Batch Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; } = 0;
}

// Transfer.cs
public class Transfer : BaseEntity
{
    public int TenantId { get; set; }
    public TransferType Type { get; set; }     // Incoming, Outgoing, Internal, Production
    public int? FromWarehouseId { get; set; }
    public Warehouse? FromWarehouse { get; set; }
    public int? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public int? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public ICollection<TransferItem> Items { get; set; }
}

public enum TransferType { Incoming = 1, Outgoing = 2, Internal = 3, ProductionOutput = 4 }
public enum TransferStatus { Pending = 1, Confirmed = 2, Rejected = 3, Cancelled = 4 }

// TransferItem.cs
public class TransferItem : BaseEntity
{
    public int TransferId { get; set; }
    public Transfer Transfer { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}
```

---

### 4.6 Production

```csharp
// ProductionStage.cs (dynamic stages — admin configures)
public class ProductionStage : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }           // "Mixing", "Cooking", "Freezing", "Packaging"
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}

// ProductionRecipe.cs (recipe for a finished product)
public class ProductionRecipe : BaseEntity
{
    public int TenantId { get; set; }
    public int OutputProductId { get; set; }   // finished product
    public Product OutputProduct { get; set; }
    public string Name { get; set; }           // "Plombir 100ml Recipe"
    public decimal OutputQuantity { get; set; }
    public int OutputUnitId { get; set; }
    public Unit OutputUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeStage> RecipeStages { get; set; }
}

// RecipeStage.cs (one stage of a recipe)
public class RecipeStage : BaseEntity
{
    public int RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; }
    public int StageId { get; set; }
    public ProductionStage Stage { get; set; }
    public int OrderNumber { get; set; }
    // Output of this stage
    public int? OutputProductId { get; set; }  // semi-finished product (null if last stage)
    public Product? OutputProduct { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    // Can the output of this stage go to warehouse? (exception case)
    public bool AllowWarehouseOutput { get; set; } = false;
    public int? OutputWarehouseId { get; set; }
    public Warehouse? OutputWarehouse { get; set; }
    public ICollection<RecipeStageItem> Inputs { get; set; }
}

// RecipeStageItem.cs (inputs for a recipe stage)
public class RecipeStageItem : BaseEntity
{
    public int RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; }
    public int ProductId { get; set; }         // raw material or semi-finished
    public Product Product { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; }
}

// ProductionOrder.cs
public class ProductionOrder : BaseEntity
{
    public int TenantId { get; set; }
    public int RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; }
    public decimal PlannedQuantity { get; set; }
    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public string? Note { get; set; }
    public ICollection<StageExecution> StageExecutions { get; set; }
}

public enum ProductionOrderStatus { Draft = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

// StageExecution.cs (actual execution of each stage)
public class StageExecution : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public int RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; } = 0;   // brak
    public decimal ReworkQuantity { get; set; } = 0;  // qayta ishlash
    public int? WorkerUserId { get; set; }
    public User? WorkerUser { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public StageExecutionStatus Status { get; set; } = StageExecutionStatus.Pending;
    public string? Note { get; set; }
    public ICollection<QcCheck> QcChecks { get; set; }
}

public enum StageExecutionStatus { Pending = 1, InProgress = 2, Completed = 3, Skipped = 4 }
```

---

### 4.7 Quality Control

```csharp
// QcParameter.cs (admin defines parameters: temperature, weight, color...)
public class QcParameter : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }           // "Temperature", "Weight", "Color"
    public string? Unit { get; set; }          // "°C", "kg", "-"
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public QcParameterType ValueType { get; set; } // Numeric, Text, Boolean
}

public enum QcParameterType { Numeric = 1, Text = 2, Boolean = 3 }

// QcCheck.cs
public class QcCheck : BaseEntity
{
    public int TenantId { get; set; }
    public int? StageExecutionId { get; set; }   // production stage check
    public StageExecution? StageExecution { get; set; }
    public int? TransferId { get; set; }          // incoming goods check
    public Transfer? Transfer { get; set; }
    public int ParameterId { get; set; }
    public QcParameter Parameter { get; set; }
    public string Value { get; set; }             // stored as string, parsed by ValueType
    public bool IsPassed { get; set; }
    public int CheckedByUserId { get; set; }
    public User CheckedByUser { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? Note { get; set; }
}
```

---

### 4.8 Finance

```csharp
// Transaction.cs
public class Transaction : BaseEntity
{
    public int TenantId { get; set; }
    public TransactionType Type { get; set; }  // Income, Expense
    public int? CounterpartyId { get; set; }
    public Counterparty? Counterparty { get; set; }
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; }
}

public enum TransactionType { Income = 1, Expense = 2 }

// Debt.cs (current debt snapshot per counterparty)
public class Debt : BaseEntity
{
    public int TenantId { get; set; }
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; }
    // positive = counterparty owes us, negative = we owe counterparty
    public decimal Amount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// PaymentHistory.cs
public class PaymentHistory : BaseEntity
{
    public int TenantId { get; set; }
    public int CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; }
    public int? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }  // Cash, Bank, Card
    public DateTime PaidAt { get; set; }
    public string? Note { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; }
}

public enum PaymentMethod { Cash = 1, Bank = 2, Card = 3 }
```

---

### 4.9 KPI & Shifts

```csharp
// Shift.cs
public class Shift : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; }           // "Morning", "Evening", "Night"
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

// ShiftPlan.cs (planned output for a shift)
public class ShiftPlan : BaseEntity
{
    public int TenantId { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public decimal PlannedQuantity { get; set; }
    public DateTime Date { get; set; }
}

// ShiftActual.cs (actual output for a shift)
public class ShiftActual : BaseEntity
{
    public int TenantId { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; } = 0;
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}

// AttendanceLog.cs (Face ID ready)
public class AttendanceLog : BaseEntity
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public AttendanceMethod Method { get; set; } // PIN, FaceID, Manual
    public string? DeviceId { get; set; }        // for future Face ID device
}

public enum AttendanceMethod { PIN = 1, FaceID = 2, Manual = 3 }
```

---

## 5. PROJECT INITIALIZATION

### Folder structure
```
wms/                    ← git init here
├── CLAUDE.md
├── .gitignore
├── backend/
└── frontend/
```

### .gitignore (root)
```
# .NET
backend/bin/
backend/obj/
backend/*.db
backend/**/*.db-shm
backend/**/*.db-wal

# Angular
frontend/node_modules/
frontend/dist/
frontend/.angular/

# IDE
.idea/
.vscode/
*.user
```

### global.json (REQUIRED — prevents SDK version conflicts)
Create next to WMS.sln:
```json
{
  "sdk": {
    "version": "8.0.417",
    "rollForward": "latestPatch"
  }
}
```
> This locks the project to .NET 8 SDK. Never remove this file.
```bash
cd wms
git init
git add .
git commit -m "initial: CLAUDE.md and project structure"
```

---

## 6. BACKEND IMPLEMENTATION

### 5.1 Project Setup

```bash
# Create solution and projects
dotnet new sln -n WMS
dotnet new classlib -n WMS.Domain
dotnet new classlib -n WMS.Application
dotnet new classlib -n WMS.Infrastructure
dotnet new webapi -n WMS.API

dotnet sln add WMS.Domain WMS.Application WMS.Infrastructure WMS.API

# Project references
dotnet add WMS.Application reference WMS.Domain
dotnet add WMS.Infrastructure reference WMS.Application
dotnet add WMS.API reference WMS.Infrastructure WMS.Application

# NuGet packages
dotnet add WMS.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add WMS.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add WMS.API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add WMS.API package Swashbuckle.AspNetCore
dotnet add WMS.Application package BCrypt.Net-Next
```

### 5.2 DbContext

```csharp
// WMS.Infrastructure/Persistence/WmsDbContext.cs
public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }

    // Tenant & Auth
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();

    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();

    // Counterparties
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    // Warehouse
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferItem> TransferItems => Set<TransferItem>();

    // Production
    public DbSet<ProductionStage> ProductionStages => Set<ProductionStage>();
    public DbSet<ProductionRecipe> ProductionRecipes => Set<ProductionRecipe>();
    public DbSet<RecipeStage> RecipeStages => Set<RecipeStage>();
    public DbSet<RecipeStageItem> RecipeStageItems => Set<RecipeStageItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<StageExecution> StageExecutions => Set<StageExecution>();

    // Quality
    public DbSet<QcParameter> QcParameters => Set<QcParameter>();
    public DbSet<QcCheck> QcChecks => Set<QcCheck>();

    // Finance
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();

    // KPI
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftPlan> ShiftPlans => Set<ShiftPlan>();
    public DbSet<ShiftActual> ShiftActuals => Set<ShiftActual>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);

        // Global soft-delete filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
            }
        }

        // Seed Modules
        modelBuilder.Entity<Module>().HasData(
            new Module { Id = 1, Name = "Raw Material Warehouse", Code = "WAREHOUSE_RAW", OrderNumber = 1 },
            new Module { Id = 2, Name = "Production", Code = "PRODUCTION", OrderNumber = 2 },
            new Module { Id = 3, Name = "Finished Goods Warehouse", Code = "WAREHOUSE_FINISHED", OrderNumber = 3 },
            new Module { Id = 4, Name = "Transfer System", Code = "TRANSFERS", OrderNumber = 4 },
            new Module { Id = 5, Name = "Finance", Code = "FINANCE", OrderNumber = 5 },
            new Module { Id = 6, Name = "KPI & Shifts", Code = "KPI", OrderNumber = 6 },
            new Module { Id = 7, Name = "Suppliers", Code = "SUPPLIERS", OrderNumber = 7 },
            new Module { Id = 8, Name = "Clients", Code = "CLIENTS", OrderNumber = 8 },
            new Module { Id = 9, Name = "Quality Control", Code = "QUALITY", OrderNumber = 9 }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
```

### 5.3 Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<WmsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// JWT Auth
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<ICounterpartyService, CounterpartyService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
    db.Database.Migrate();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
```

### 5.4 appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=wms.db"
  },
  "Jwt": {
    "Key": "your-super-secret-key-min-32-chars-long!!"
  },
  "AllowedHosts": "*"
}
```

---

## 6. API ENDPOINTS

All endpoints require `Authorization: Bearer {token}` except auth endpoints.
All endpoints are prefixed with `/api`.

### 6.1 Auth
```
POST   /api/auth/login           - { phone, password, tenantSlug } → { token, user }
POST   /api/auth/refresh         - refresh token
GET    /api/auth/me              - current user info
```

### 6.2 Tenants (Super Admin only)
```
GET    /api/tenants              - list all tenants
POST   /api/tenants              - create tenant
PUT    /api/tenants/{id}         - update tenant
DELETE /api/tenants/{id}         - soft delete
GET    /api/tenants/{id}/modules - get tenant modules
PUT    /api/tenants/{id}/modules - toggle modules
```

### 6.3 Users
```
GET    /api/users                - list (filtered by tenantId from token)
POST   /api/users                - create
PUT    /api/users/{id}           - update
DELETE /api/users/{id}           - soft delete
PUT    /api/users/{id}/roles     - assign roles
```

### 6.4 Products
```
GET    /api/products             - list (with category, unit)
POST   /api/products             - create
PUT    /api/products/{id}        - update
DELETE /api/products/{id}        - soft delete
GET    /api/categories           - list categories (tree)
POST   /api/categories           - create
GET    /api/units                - list units
POST   /api/units                - create
```

### 6.5 Counterparties
```
GET    /api/counterparties       - list (?type=Supplier|Client|Both)
POST   /api/counterparties       - create
PUT    /api/counterparties/{id}  - update
DELETE /api/counterparties/{id}  - soft delete
GET    /api/counterparties/{id}/balance    - current balance
GET    /api/counterparties/{id}/payments   - payment history
```

### 6.6 Warehouses
```
GET    /api/warehouses           - list
POST   /api/warehouses           - create
PUT    /api/warehouses/{id}      - update
GET    /api/warehouses/{id}/stock          - current stock (grouped by product)
GET    /api/warehouses/{id}/stock/detail   - detailed (by batch/location)
GET    /api/locations            - list (?warehouseId=)
POST   /api/locations            - create
```

### 6.7 Transfers
```
GET    /api/transfers            - list (?type=&status=&from=&to=)
POST   /api/transfers            - create transfer
GET    /api/transfers/{id}       - get with items
PUT    /api/transfers/{id}/confirm    - confirm (updates stock)
PUT    /api/transfers/{id}/reject     - reject
DELETE /api/transfers/{id}       - cancel (if Pending)
```

### 6.8 Production
```
GET    /api/production/stages    - list stages
POST   /api/production/stages    - create stage
PUT    /api/production/stages/{id}        - update
DELETE /api/production/stages/{id}        - delete
PATCH  /api/production/stages/reorder     - reorder (drag & drop)

GET    /api/production/recipes   - list recipes
POST   /api/production/recipes   - create recipe with stages + items
GET    /api/production/recipes/{id}       - full recipe detail
PUT    /api/production/recipes/{id}       - update
DELETE /api/production/recipes/{id}       - soft delete

GET    /api/production/orders    - list (?status=)
POST   /api/production/orders    - create order
GET    /api/production/orders/{id}        - full detail with stage executions
PUT    /api/production/orders/{id}/start  - start order
PUT    /api/production/orders/{id}/stages/{stageId}/execute  - log stage execution
PUT    /api/production/orders/{id}/complete                  - complete order
```

### 6.9 Finance
```
GET    /api/finance/transactions - list (?type=&from=&to=)
POST   /api/finance/transactions - create
GET    /api/finance/debts        - all debts summary
POST   /api/finance/payments     - record payment
GET    /api/finance/summary      - total income/expense/debt KPIs
```

### 6.10 KPI
```
GET    /api/shifts               - list
POST   /api/shifts               - create
GET    /api/kpi/plans            - list shift plans
POST   /api/kpi/plans            - create plan
GET    /api/kpi/actuals          - list actuals
POST   /api/kpi/actuals          - record actual
GET    /api/kpi/summary          - planned vs actual comparison
GET    /api/kpi/efficiency       - efficiency % by shift/product/date
POST   /api/attendance/checkin   - check in
PUT    /api/attendance/{id}/checkout   - check out
GET    /api/attendance           - list (?userId=&date=)
```

### 6.11 Quality Control
```
GET    /api/qc/parameters        - list parameters
POST   /api/qc/parameters        - create
POST   /api/qc/checks            - record check
GET    /api/qc/checks            - list (?transferId=&stageExecutionId=)
```

### 6.12 Counterparty Portal
```
POST   /api/portal/login         - { phone, password } → { token, counterparty }
GET    /api/portal/me            - current counterparty info
GET    /api/portal/transfers     - own transfers (paginated)
GET    /api/portal/transfers/:id - transfer detail
GET    /api/portal/finance       - own balance + debt summary
GET    /api/portal/payments      - own payment history
```

> Portal uses separate JWT claims: `counterpartyId`, `tenantId`, `type (Supplier/Client)`
> Portal endpoints use `[Authorize(Policy = "PortalOnly")]`

---

## 7. FRONTEND IMPLEMENTATION

### 7.1 Setup

```bash
npm install -g @angular/cli@21
ng new frontend --standalone --style=scss --routing --ssr=false
cd frontend

# PrimeNG 21 (Angular 21 compatible)
npm install primeng@21.1.5 primeicons

# Tailwind CSS v4
npm install tailwindcss @tailwindcss/vite

# Transloco
npm install @jsverse/transloco

# HTTP
# Already included in Angular

# Charts
npm install apexcharts ng-apexcharts
```

### 7.2 Angular Configuration

```typescript
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, tenantInterceptor])),
    provideAnimationsAsync(),
    provideTransloco({
      config: {
        availableLangs: ['uz', 'ru'],
        defaultLang: 'uz',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader
    }),
    // PrimeNG
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: '.dark-mode' }
      }
    })
  ]
};
```

### 7.3 Auth Interceptor

```typescript
// core/interceptors/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req).pipe(
    catchError(err => {
      if (err.status === 401) inject(AuthService).logout();
      return throwError(() => err);
    })
  );
};
```

### 7.4 Module Guard (check if module is enabled)

```typescript
// core/guards/module.guard.ts
export const moduleGuard = (moduleCode: string): CanActivateFn => {
  return () => {
    const tenantService = inject(TenantService);
    const router = inject(Router);
    if (tenantService.isModuleEnabled(moduleCode)) return true;
    router.navigate(['/dashboard']);
    return false;
  };
};
```

### 7.5 App Routes

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./modules/auth/auth.routes')
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./modules/dashboard/dashboard.component')
      },
      {
        path: 'warehouse',
        canActivate: [moduleGuard('WAREHOUSE_RAW')],
        loadChildren: () => import('./modules/warehouse/warehouse.routes')
      },
      {
        path: 'production',
        canActivate: [moduleGuard('PRODUCTION')],
        loadChildren: () => import('./modules/production/production.routes')
      },
      {
        path: 'transfers',
        canActivate: [moduleGuard('TRANSFERS')],
        loadChildren: () => import('./modules/transfers/transfers.routes')
      },
      {
        path: 'finance',
        canActivate: [moduleGuard('FINANCE')],
        loadChildren: () => import('./modules/finance/finance.routes')
      },
      {
        path: 'kpi',
        canActivate: [moduleGuard('KPI')],
        loadChildren: () => import('./modules/kpi/kpi.routes')
      },
      {
        path: 'counterparties',
        loadChildren: () => import('./modules/counterparties/counterparties.routes')
      },
      {
        path: 'products',
        loadChildren: () => import('./modules/products/products.routes')
      },
      {
        path: 'settings',
        loadChildren: () => import('./modules/settings/settings.routes')
      }
    ]
  },
  // Counterparty self-service portal (separate layout, no sidebar)
  {
    path: 'portal',
    children: [
      { path: 'login', loadComponent: () => import('./modules/portal/portal-login.component') },
      {
        path: '',
        canActivate: [portalGuard],
        loadChildren: () => import('./modules/portal/portal.routes')
      }
    ]
  }
];
```

### 7.6 Shell Layout

```
Shell Component:
  ├── Sidebar (left, collapsible)
  │   ├── Logo + Tenant name
  │   ├── Nav items (only enabled modules shown)
  │   └── User info + logout
  └── Main content area
      ├── Header (page title, breadcrumb, dark/light toggle, lang switcher)
      └── <router-outlet>
```

Sidebar nav items are built dynamically from `TenantService.enabledModules()` signal.

### 7.7 Core Services

```typescript
// core/services/auth.service.ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  token = signal<string | null>(localStorage.getItem('token'));
  currentUser = signal<User | null>(null);

  login(credentials: LoginDto) {
    return this.http.post<AuthResponse>('/api/auth/login', credentials).pipe(
      tap(res => {
        this.token.set(res.token);
        this.currentUser.set(res.user);
        localStorage.setItem('token', res.token);
      })
    );
  }

  logout() {
    this.token.set(null);
    this.currentUser.set(null);
    localStorage.removeItem('token');
    inject(Router).navigate(['/auth/login']);
  }
}

// core/services/tenant.service.ts
@Injectable({ providedIn: 'root' })
export class TenantService {
  enabledModules = signal<string[]>([]);

  isModuleEnabled(code: string): boolean {
    return this.enabledModules().includes(code);
  }

  loadModules() {
    // called after login, loads enabled modules for current tenant
  }
}
```

---

## 8. FRONTEND PAGES (per module)

### 8.1 Dashboard
- Summary cards: total stock, active orders, today transfers, pending payments
- Charts: production plan vs actual (ApexCharts line), weekly transfers (bar), top products (pie)
- Recent transfers list
- Low stock alerts

### 8.2 Warehouse Module
Pages:
- `/warehouse` — stock overview (table with product, batch, location, qty, expiry)
- `/warehouse/movements` — stock movement history
- `/warehouse/locations` — manage locations (CRUD)
- `/warehouse/batches` — batch/lot list with expiry status

### 8.3 Production Module
Pages:
- `/production/stages` — manage stages (CRUD + drag reorder)
- `/production/recipes` — recipe list
- `/production/recipes/new` — recipe builder (add stages, add inputs per stage)
- `/production/recipes/:id` — recipe detail
- `/production/orders` — production order list
- `/production/orders/new` — create order
- `/production/orders/:id` — order detail + stage execution logger
  - Each stage: enter actual qty, waste qty, rework qty
  - Mark stage as complete → next stage unlocks

### 8.4 Transfers Module
Pages:
- `/transfers` — list with filters (type, status, date range)
- `/transfers/new` — create transfer (select type → fill form)
- `/transfers/:id` — detail + confirm/reject buttons
- Transfer types: Incoming (from supplier), Outgoing (to client), Internal (warehouse to warehouse)

### 8.5 Finance Module
Pages:
- `/finance` — summary (income, expense, total debt)
- `/finance/transactions` — transaction list + add
- `/finance/debts` — debt list per counterparty
- `/finance/payments` — record payment, view payment history

### 8.6 KPI Module
Pages:
- `/kpi` — dashboard (efficiency %, plan vs actual charts by ApexCharts)
- `/kpi/shifts` — manage shifts
- `/kpi/plans` — enter shift plans
- `/kpi/actuals` — enter actuals
- `/kpi/attendance` — attendance log + check-in/out

### 8.7 Counterparties Module
Pages:
- `/counterparties/suppliers` — supplier list
- `/counterparties/clients` — client list
- `/counterparties/:id` — detail (balance, transfer history, payment history)

### 8.8 Products Module
Pages:
- `/products` — product list (filter by type: Raw/Semi/Finished)
- `/products/categories` — category tree management
- `/products/units` — unit management

### 8.9 Settings Module
Pages:
- `/settings/users` — user management + role assignment
- `/settings/roles` — role CRUD
- `/settings/modules` — enable/disable modules (toggle list)
- `/settings/qc-parameters` — QC parameter CRUD
- `/settings/profile` — current user profile

### 8.10 Counterparty Portal (separate layout)
Portal has its own simple layout — no sidebar, minimal header (logo + logout).

Pages:
- `/portal/login` — phone + password login (separate from main app login)
- `/portal/dashboard` — welcome + balance summary card + last 5 transfers
- `/portal/transfers` — own transfer list (read-only, with status badges)
- `/portal/transfers/:id` — transfer detail (products, quantities, prices)
- `/portal/finance` — current balance, total debt, payment history

Portal rules:
- Only accessible if `Counterparty.PortalEnabled = true`
- Uses separate JWT token (`portalToken` in localStorage)
- Read-only — counterparty cannot create or modify anything (Phase 1)
- Admin enables portal per counterparty in Settings → Counterparties → Edit
- `portalGuard` checks `portalToken` — if missing, redirect to `/portal/login`

---

## 9. UI LIBRARIES & NOTIFICATIONS

### Icons — PrimeIcons (built-in, no install needed)
```html
<!-- Usage -->
<i class="pi pi-home"></i>
<i class="pi pi-box"></i>
<i class="pi pi-chart-bar"></i>
<i class="pi pi-check-circle"></i>
<i class="pi pi-times-circle"></i>
```

Common icons used in this project:
| Usage | Icon class |
|---|---|
| Dashboard | `pi-th-large` |
| Warehouse | `pi-box` |
| Production | `pi-cog` |
| Transfers | `pi-arrow-right-arrow-left` |
| Finance | `pi-wallet` |
| KPI | `pi-chart-line` |
| Suppliers | `pi-truck` |
| Clients | `pi-users` |
| Settings | `pi-sliders-h` |
| Add | `pi-plus` |
| Edit | `pi-pencil` |
| Delete | `pi-trash` |
| Confirm | `pi-check` |
| Alert | `pi-exclamation-triangle` |
| Success | `pi-check-circle` |
| Logout | `pi-sign-out` |

---

### Notifications — PrimeNG Toast (replaces SweetAlert + Toastr)

PrimeNG has everything built-in. No need for SweetAlert or ngx-toastr.

**Setup in app.config.ts:**
```typescript
providers: [
  ...
  MessageService,   // for Toast
  ConfirmationService  // for ConfirmDialog
]
```

**Add to app root component:**
```html
<p-toast position="top-right" [life]="4000" />
<p-confirmDialog />
```

**Create NotificationService wrapper:**
```typescript
// shared/services/notification.service.ts
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private msg = inject(MessageService);
  private confirm = inject(ConfirmationService);

  success(message: string) {
    this.msg.add({ severity: 'success', summary: 'Success', detail: message });
  }

  error(message: string) {
    this.msg.add({ severity: 'error', summary: 'Error', detail: message });
  }

  warn(message: string) {
    this.msg.add({ severity: 'warn', summary: 'Warning', detail: message });
  }

  info(message: string) {
    this.msg.add({ severity: 'info', summary: 'Info', detail: message });
  }

  // Replaces SweetAlert confirm dialog
  confirmDelete(message: string, onAccept: () => void) {
    this.confirm.confirm({
      message,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-text',
      accept: onAccept
    });
  }

  confirmAction(message: string, header: string, onAccept: () => void) {
    this.confirm.confirm({ message, header, icon: 'pi pi-question-circle', accept: onAccept });
  }
}
```

**Usage in any component:**
```typescript
// inject and use
private notify = inject(NotificationService);

save() {
  this.service.save(data).subscribe({
    next: () => this.notify.success('Saved successfully'),
    error: () => this.notify.error('Something went wrong')
  });
}

delete(id: number) {
  this.notify.confirmDelete('Are you sure?', () => {
    this.service.delete(id).subscribe(() => this.notify.success('Deleted'));
  });
}
```

---

### SHARED COMPONENTS

Build these reusable components in `shared/components/`:

| Component | Description |
|---|---|
| `ConfirmDialogComponent` | PrimeNG ConfirmDialog wrapper |
| `DataTableComponent` | PrimeNG Table with pagination, sort, filter |
| `PageHeaderComponent` | Page title + breadcrumb + action buttons slot |
| `StatusBadgeComponent` | Colored badge for Transfer/Order status |
| `EmptyStateComponent` | Empty list illustration + message |
| `LoadingSpinnerComponent` | Full-page or inline loading |
| `SearchInputComponent` | Debounced search input |
| `DateRangePickerComponent` | From/To date picker |

---

## 10. THEME, DESIGN & UI/UX REQUIREMENTS

> **CRITICAL**: UI must be modern, elegant, refined, and intuitive.
> Any ordinary person — not just IT users — must feel comfortable using this system.
> Every screen must look like a premium product. No generic or plain styling allowed.

---

### 10.1 Design Principles

- **Clean & Airy**: generous white space, no visual clutter
- **Consistent**: same spacing, radius, shadow system everywhere
- **Responsive**: works on desktop (primary), tablet, and mobile
- **Accessible**: readable fonts, sufficient contrast, clear labels
- **Fast-feeling**: skeleton loaders, smooth transitions, no jarring jumps
- **Delightful**: subtle animations on cards, hover effects, smooth page transitions

---

### 10.2 Color System

```scss
/* styles.scss — CSS variables */
:root {
  /* Brand */
  --primary-50:  #eef2ff;
  --primary-100: #e0e7ff;
  --primary-500: #6366f1;   /* main indigo */
  --primary-600: #4f46e5;
  --primary-700: #4338ca;

  /* Semantic */
  --success: #10b981;
  --warning: #f59e0b;
  --danger:  #ef4444;
  --info:    #3b82f6;

  /* Neutrals (light mode) */
  --bg-base:    #f8fafc;    /* page background */
  --bg-surface: #ffffff;    /* card background */
  --bg-muted:   #f1f5f9;    /* subtle bg */
  --border:     #e2e8f0;
  --text-primary:   #0f172a;
  --text-secondary: #64748b;
  --text-muted:     #94a3b8;

  /* Shadows */
  --shadow-sm: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);
  --shadow-md: 0 4px 6px rgba(0,0,0,0.07), 0 2px 4px rgba(0,0,0,0.05);
  --shadow-lg: 0 10px 15px rgba(0,0,0,0.08), 0 4px 6px rgba(0,0,0,0.05);

  /* Radius */
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 16px;
  --radius-xl: 20px;
}

/* Dark mode */
.dark-mode {
  --bg-base:    #0f172a;
  --bg-surface: #1e293b;
  --bg-muted:   #334155;
  --border:     #334155;
  --text-primary:   #f1f5f9;
  --text-secondary: #94a3b8;
  --text-muted:     #64748b;
}
```

---

### 10.3 Typography

```scss
/* Use Google Fonts — add to index.html */
/* Primary: DM Sans (clean, modern, readable) */
/* Mono: JetBrains Mono (numbers, codes) */

@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap');

body {
  font-family: 'DM Sans', sans-serif;
  font-size: 14px;
  line-height: 1.6;
  color: var(--text-primary);
  background: var(--bg-base);
}

h1 { font-size: 28px; font-weight: 700; }
h2 { font-size: 22px; font-weight: 600; }
h3 { font-size: 18px; font-weight: 600; }

/* Numbers, quantities — monospace */
.number, .qty, .amount {
  font-family: 'JetBrains Mono', monospace;
  font-weight: 500;
}
```

---

### 10.4 Layout & Spacing

```
Sidebar: 260px wide (collapsed: 72px — icon only)
Header: 64px height, sticky
Content area: padding 24px (desktop), 16px (tablet), 12px (mobile)
Card padding: 20px
Grid gap: 20px
```

Sidebar behavior:
- Desktop: always visible, collapsible to icon-only mode
- Tablet: overlay drawer
- Mobile: bottom sheet or hamburger drawer

---

### 10.5 Component Styling Rules

**Cards:**
```scss
.wms-card {
  background: var(--bg-surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  padding: 20px;
  transition: box-shadow 0.2s ease;

  &:hover {
    box-shadow: var(--shadow-md);
  }
}
```

**Summary/KPI cards (dashboard top row):**
- Icon with colored background circle (soft tint)
- Large bold number (monospace font)
- Label below
- Trend indicator: green ↑ or red ↓ with % change

**Tables (PrimeNG p-table):**
- Striped rows (alternating subtle bg)
- Sticky header
- Row hover highlight
- Status columns: always use colored badge, never plain text
- Numbers: right-aligned, monospace

**Buttons:**
- Primary: filled indigo, rounded-md, subtle shadow
- Secondary: outlined, border + text
- Danger: red filled for destructive actions
- All: smooth hover transition (scale 1.01 + shadow lift)

**Forms:**
- Floating labels or clear top labels
- Inline validation messages (red, small)
- Required fields marked with subtle asterisk
- Group related fields visually

**Status Badges:**
```scss
/* Use semantic colors with soft background */
.badge-success { background: #d1fae5; color: #065f46; }
.badge-warning { background: #fef3c7; color: #92400e; }
.badge-danger  { background: #fee2e2; color: #991b1b; }
.badge-info    { background: #dbeafe; color: #1e40af; }
.badge-neutral { background: #f1f5f9; color: #475569; }
```

---

### 10.6 Sidebar Design

```
┌─────────────────┐
│  🏭 WMS          │  ← Logo + brand name
│  Sarvar Zavod   │  ← Tenant name (small, muted)
├─────────────────┤
│                 │
│ 📊 Dashboard    │  ← Active: indigo bg, white text
│ 📦 Warehouse    │
│ ⚙️  Production  │
│ 🔄 Transfers    │
│ 💰 Finance      │
│ 📈 KPI          │
│ 👥 Partners     │
│ 🛍️  Products    │
│                 │
├─────────────────┤
│ ⚙️  Settings    │  ← Bottom group
│ 👤 G'olibjon    │  ← User avatar + name
│    [Logout]     │
└─────────────────┘
```

- Active item: indigo background, rounded-md, white text
- Inactive: transparent, muted text, hover: light indigo tint
- Section dividers with tiny muted labels
- Smooth collapse animation (width transition)
- Hidden modules: completely absent from sidebar (not greyed out)

---

### 10.7 Dashboard Page Design

```
┌─────────────────────────────────────────────────────┐
│  Good morning, G'olibjon 👋    [date]  [🔔] [🌙]   │  ← Header
├─────────┬─────────┬─────────┬─────────┬─────────────┤
│  📦      │  🏭      │  🔄      │  💰      │             │
│ 1,240kg  │ 85%     │  12     │ $4,200   │  Low stock  │
│ In stock │ Effic.  │ Active  │ Revenue  │  ⚠️ 3 items  │
├─────────┴─────────┴─────────┴──────────┴────────────┤
│                                                      │
│  Production: Plan vs Actual (Line Chart — 7 days)   │
│                                                      │
├──────────────────────┬───────────────────────────────┤
│  Transfers (Bar)     │  Product Distribution (Donut) │
│  Last 7 days         │  By type                      │
├──────────────────────┴───────────────────────────────┤
│  Recent Transfers (Table — last 5, with status)      │
└──────────────────────────────────────────────────────┘
```

---

### 10.8 ApexCharts Configuration

Install:
```bash
npm install apexcharts ng-apexcharts
```

**Global ApexCharts defaults** — create `core/config/apex-defaults.ts`:

```typescript
import { ApexChart, ApexTheme, ApexGrid, ApexTooltip } from 'ng-apexcharts';

export const APEX_DEFAULTS = {
  chart: {
    fontFamily: 'DM Sans, sans-serif',
    toolbar: { show: false },
    animations: {
      enabled: true,
      easing: 'easeinout',
      speed: 600,
    },
  } as ApexChart,

  theme: {
    mode: 'light',         // toggle with dark mode
    palette: 'palette1',
  } as ApexTheme,

  grid: {
    borderColor: '#e2e8f0',
    strokeDashArray: 4,
    xaxis: { lines: { show: false } },
  } as ApexGrid,

  tooltip: {
    theme: 'light',
    style: { fontFamily: 'DM Sans, sans-serif', fontSize: '13px' },
  } as ApexTooltip,

  // Brand colors for series
  colors: ['#6366f1', '#10b981', '#f59e0b', '#3b82f6', '#ef4444'],
};
```

**Chart usage per module:**

| Module | Chart Type | What it shows |
|---|---|---|
| Dashboard | Line | Production plan vs actual (7 days) |
| Dashboard | Bar | Daily transfers (incoming vs outgoing) |
| Dashboard | Donut | Product distribution by type |
| Production | Line | Plan vs actual per product |
| Production | Bar | Waste % per stage |
| KPI | RadialBar | Shift efficiency % (gauge style) |
| KPI | Heatmap | Worker attendance per day |
| Finance | Area | Income vs expense over time |
| Finance | Bar | Top debtors |
| Warehouse | Bar | Stock levels by product |
| Warehouse | Line | Stock movement history |

**Chart sizing:**
- Full-width charts: height 280px
- Half-width charts: height 240px
- Gauge/Radial: height 200px
- All charts: `width: '100%'` (responsive)

**Dark mode sync:**
```typescript
// When dark mode toggles, update all charts:
effect(() => {
  const isDark = this.themeService.isDark();
  ApexCharts.exec('*', 'updateOptions', {
    theme: { mode: isDark ? 'dark' : 'light' }
  });
});
```

---

### 10.9 Animations & Micro-interactions

```scss
/* Page entrance — apply to main content area */
@keyframes fadeSlideIn {
  from { opacity: 0; transform: translateY(12px); }
  to   { opacity: 1; transform: translateY(0); }
}

.page-enter {
  animation: fadeSlideIn 0.25s ease-out;
}

/* Card hover */
.wms-card {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  &:hover { transform: translateY(-2px); box-shadow: var(--shadow-lg); }
}

/* Button press */
.p-button {
  transition: transform 0.1s ease;
  &:active { transform: scale(0.97); }
}

/* Skeleton loader (while data loads) */
.skeleton {
  background: linear-gradient(90deg, var(--bg-muted) 25%, var(--border) 50%, var(--bg-muted) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.4s infinite;
  border-radius: var(--radius-sm);
}

@keyframes shimmer {
  0%   { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

---

### 10.10 PrimeNG Overrides

```scss
/* Override PrimeNG with Tailwind and custom vars */

/* Table */
.p-datatable {
  .p-datatable-thead > tr > th {
    background: var(--bg-muted);
    color: var(--text-secondary);
    font-size: 12px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 1px solid var(--border);
  }
  .p-datatable-tbody > tr {
    transition: background 0.15s ease;
    &:hover { background: var(--bg-muted); }
    > td { border-bottom: 1px solid var(--border); }
  }
}

/* Input */
.p-inputtext {
  border-radius: var(--radius-sm) !important;
  border-color: var(--border) !important;
  font-family: 'DM Sans', sans-serif !important;
  &:focus { border-color: var(--primary-500) !important; box-shadow: 0 0 0 3px rgba(99,102,241,0.15) !important; }
}

/* Button */
.p-button {
  border-radius: var(--radius-sm) !important;
  font-family: 'DM Sans', sans-serif !important;
  font-weight: 600 !important;
}

/* Dialog */
.p-dialog {
  border-radius: var(--radius-xl) !important;
  box-shadow: var(--shadow-lg) !important;
  .p-dialog-header { border-radius: var(--radius-xl) var(--radius-xl) 0 0 !important; }
}

/* Tag/Badge */
.p-tag { border-radius: 20px !important; font-weight: 600 !important; }
```

---

### 10.11 i18n (Transloco)

Translation files go in `assets/i18n/`:
- `uz.json` — Uzbek (default)
- `ru.json` — Russian

Key translation namespaces:
```json
{
  "common": { "save": "Saqlash", "cancel": "Bekor", "delete": "O'chirish", "confirm": "Tasdiqlash", "search": "Qidirish" },
  "nav": { "dashboard": "Bosh sahifa", "warehouse": "Ombor", "production": "Ishlab chiqarish" },
  "status": { "pending": "Kutilmoqda", "confirmed": "Tasdiqlangan", "rejected": "Rad etilgan" }
}
```

Language switcher: stored in `localStorage('lang')`, applied on app init.

---

## 16. DEPLOYMENT (Ubuntu Server)

```bash
# Backend
cd backend
dotnet publish WMS.API -c Release -o /var/www/wms-api

# Create systemd service
# /etc/systemd/system/wms-api.service
[Unit]
Description=WMS API
After=network.target

[Service]
WorkingDirectory=/var/www/wms-api
ExecStart=/usr/bin/dotnet /var/www/wms-api/WMS.API.dll
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:7040

[Install]
WantedBy=multi-user.target

# Frontend
cd frontend
ng build --configuration production
# Copy dist/ to /var/www/wms-ui

# Nginx config
server {
    listen 80;
    server_name wms.yourdomain.uz;

    # Frontend
    location / {
        root /var/www/wms-ui/browser;
        try_files $uri $uri/ /index.html;
    }

    # Backend API
    location /api {
        proxy_pass http://localhost:7040;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

Follow this exact order:

### Phase 1 — Foundation (Backend)
1. Create solution and projects
2. Define all entities in `WMS.Domain`
3. Configure `WmsDbContext` with all DbSets and Fluent API
4. Run initial migration and verify schema
5. Implement `AuthService` + JWT
6. Create `AuthController`
7. Test login with Swagger

### Phase 2 — Core Backend
8. Implement `ProductService` + `ProductController`
9. Implement `CounterpartyService` + `CounterpartyController`
10. Implement `WarehouseService` + `WarehouseController`
11. Implement `TransferService` + `TransferController` (with stock update logic)
12. Implement `ProductionService` + `ProductionController`
13. Implement `FinanceService` + `FinanceController`
14. Implement `KpiService` + `KpiController`
15. Implement `QcService` + `QcController`
16. Implement `PortalAuthService` + `PortalController` (counterparty portal API)
17. Create Analytics DTOs + `IAnalyticsService` + `AnalyticsService` + `AnalyticsController`
18. Register all services in DI (Program.cs)

### Phase 3 — Foundation (Frontend)
15. Create Angular app with configuration
16. Implement `AuthService`, login page, auth guard
17. Build `ShellComponent` (sidebar + header layout)
18. Build dynamic sidebar with module-based nav
19. Implement API service base class

### Phase 4 — Feature Pages (Frontend)
20. Create `AnalyticsService` (Angular) + TypeScript interfaces
21. Dashboard page with summary cards + ApexCharts
22. Products module (list, CRUD)
23. Counterparties module (list, detail, balance) + portal toggle
24. Warehouse module (stock view, movements, batches) + charts
25. Transfers module (list, create, confirm/reject)
26. Production module (stages, recipes, orders, execution) + charts
27. Finance module (transactions, debts, payments) + charts
28. KPI module (shifts, plans, actuals, attendance) + charts
29. Settings module (users, roles, modules toggle, QC params)
30. Portal module (login, dashboard, transfers, finance) — separate layout

### Phase 5 — Polish
29. Add Transloco i18n (uz + ru)
30. Dark/light mode toggle
31. Loading states and error handling
32. Form validation messages
33. Mobile responsive adjustments

---

## 12. BUSINESS LOGIC RULES

### Transfer → Stock Update
- When transfer is **Confirmed**:
  - `Incoming`: add to `WarehouseStock` (create Batch if not exists)
  - `Outgoing`: subtract from `WarehouseStock`, update `Batch.RemainingQuantity`
  - `Internal`: subtract from source, add to destination
- FEFO rule: when picking from warehouse, always take the batch with earliest `ExpiryDate` first

### Production Order → Stock
- When stage is executed:
  - Inputs are deducted from `WarehouseStock`
  - If `AllowWarehouseOutput = true`: output goes to designated warehouse
  - On final stage completion: output goes to finished goods warehouse, creates new `Batch`

### Finance → Debt
- When outgoing transfer is confirmed: `Debt.Amount += totalPrice` (client owes us)
- When incoming transfer is confirmed: `Debt.Amount -= totalPrice` (we owe supplier)
- When payment is recorded: `Debt.Amount` adjusted accordingly

### KPI Efficiency
```
Efficiency % = (ActualQuantity / PlannedQuantity) * 100
Waste % = (WasteQuantity / ActualQuantity) * 100
```

---

## 13. COMMON PATTERNS

### API Response Wrapper
Always use this wrapper for all controller responses:

```csharp
// WMS.Application/Common/ApiResponse.cs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

// Usage in controller
return Ok(ApiResponse<List<ProductDto>>.Ok(products));
return BadRequest(ApiResponse<object>.Fail("Product not found"));
```

### TenantId from JWT (use in every controller)
```csharp
// Add this helper in BaseController
[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected int TenantId =>
        int.Parse(User.FindFirst("tenantId")?.Value ?? "0");

    protected int UserId =>
        int.Parse(User.FindFirst("userId")?.Value ?? "0");
}

// All controllers inherit BaseController
public class ProductsController : BaseController { ... }
```

### JWT Claims (set on login)
```csharp
var claims = new[]
{
    new Claim("tenantId", user.TenantId.ToString()),
    new Claim("userId", user.Id.ToString()),
    new Claim("fullName", user.FullName),
    new Claim(ClaimTypes.Role, roleName)
};
```

### Angular Environment
```typescript
// environments/environment.ts (development)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:7040/api'
};

// environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

### Angular API Base Service
```typescript
// core/services/api.service.ts
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  get<T>(path: string, params?: any) {
    return this.http.get<ApiResponse<T>>(`${this.base}/${path}`, { params });
  }

  post<T>(path: string, body: any) {
    return this.http.post<ApiResponse<T>>(`${this.base}/${path}`, body);
  }

  put<T>(path: string, body: any) {
    return this.http.put<ApiResponse<T>>(`${this.base}/${path}`, body);
  }

  delete<T>(path: string) {
    return this.http.delete<ApiResponse<T>>(`${this.base}/${path}`);
  }
}
```

### Angular Dev Server Port
```json
// angular.json — set port to 7050
"serve": {
  "options": {
    "port": 7050
  }
}
```

### .NET Dev Port
```json
// WMS.API/Properties/launchSettings.json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:7040"
    }
  }
}
```

---

## 15. ANALYTICS & CHARTS — BACKEND

### 15.1 Analytics Controller

```csharp
// WMS.API/Controllers/AnalyticsController.cs
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : BaseController
{
    private readonly IAnalyticsService _analytics;
    public AnalyticsController(IAnalyticsService analytics)
        => _analytics = analytics;

    // Dashboard summary cards
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(ApiResponse<DashboardSummaryDto>.Ok(
            await _analytics.GetDashboardSummary(TenantId)));

    // Production: plan vs actual (last N days)
    [HttpGet("production/plan-vs-actual")]
    public async Task<IActionResult> GetProductionPlanVsActual([FromQuery] int days = 7)
        => Ok(ApiResponse<List<PlanVsActualDto>>.Ok(
            await _analytics.GetProductionPlanVsActual(TenantId, days)));

    // Production: waste % per stage
    [HttpGet("production/waste-by-stage")]
    public async Task<IActionResult> GetWasteByStage([FromQuery] int days = 30)
        => Ok(ApiResponse<List<WasteByStageDto>>.Ok(
            await _analytics.GetWasteByStage(TenantId, days)));

    // Transfers: daily incoming vs outgoing (last N days)
    [HttpGet("transfers/daily")]
    public async Task<IActionResult> GetDailyTransfers([FromQuery] int days = 7)
        => Ok(ApiResponse<List<DailyTransferDto>>.Ok(
            await _analytics.GetDailyTransfers(TenantId, days)));

    // Warehouse: current stock levels by product
    [HttpGet("warehouse/stock-levels")]
    public async Task<IActionResult> GetStockLevels([FromQuery] int? warehouseId = null)
        => Ok(ApiResponse<List<StockLevelDto>>.Ok(
            await _analytics.GetStockLevels(TenantId, warehouseId)));

    // Warehouse: stock movement history (line chart)
    [HttpGet("warehouse/stock-history")]
    public async Task<IActionResult> GetStockHistory([FromQuery] int days = 30)
        => Ok(ApiResponse<List<StockHistoryDto>>.Ok(
            await _analytics.GetStockHistory(TenantId, days)));

    // Finance: income vs expense over time
    [HttpGet("finance/income-expense")]
    public async Task<IActionResult> GetIncomeExpense([FromQuery] int days = 30)
        => Ok(ApiResponse<List<IncomeExpenseDto>>.Ok(
            await _analytics.GetIncomeExpense(TenantId, days)));

    // Finance: top debtors
    [HttpGet("finance/top-debtors")]
    public async Task<IActionResult> GetTopDebtors([FromQuery] int top = 10)
        => Ok(ApiResponse<List<TopDebtorDto>>.Ok(
            await _analytics.GetTopDebtors(TenantId, top)));

    // KPI: shift efficiency % (radial chart)
    [HttpGet("kpi/shift-efficiency")]
    public async Task<IActionResult> GetShiftEfficiency([FromQuery] int days = 7)
        => Ok(ApiResponse<List<ShiftEfficiencyDto>>.Ok(
            await _analytics.GetShiftEfficiency(TenantId, days)));

    // KPI: attendance heatmap (worker x day)
    [HttpGet("kpi/attendance-heatmap")]
    public async Task<IActionResult> GetAttendanceHeatmap([FromQuery] int days = 30)
        => Ok(ApiResponse<List<AttendanceHeatmapDto>>.Ok(
            await _analytics.GetAttendanceHeatmap(TenantId, days)));

    // Products: distribution by type (donut chart)
    [HttpGet("products/distribution")]
    public async Task<IActionResult> GetProductDistribution()
        => Ok(ApiResponse<List<ProductDistributionDto>>.Ok(
            await _analytics.GetProductDistribution(TenantId)));
}
```

---

### 15.2 Analytics DTOs

```csharp
// WMS.Application/DTOs/Analytics/

// Dashboard summary cards
public class DashboardSummaryDto
{
    public decimal TotalStockKg { get; set; }
    public int ActiveProductionOrders { get; set; }
    public int PendingTransfers { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal RevenueChangePercent { get; set; }  // vs last month
    public int LowStockProductCount { get; set; }
    public decimal OverallEfficiencyPercent { get; set; }
    public decimal TotalDebt { get; set; }
}

// Production plan vs actual
public class PlanVsActualDto
{
    public DateTime Date { get; set; }
    public string ProductName { get; set; }
    public decimal Planned { get; set; }
    public decimal Actual { get; set; }
    public decimal EfficiencyPercent => Planned > 0
        ? Math.Round(Actual / Planned * 100, 1) : 0;
}

// Waste by stage
public class WasteByStageDto
{
    public string StageName { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalWaste { get; set; }
    public decimal WastePercent => TotalActual > 0
        ? Math.Round(TotalWaste / TotalActual * 100, 2) : 0;
}

// Daily transfers
public class DailyTransferDto
{
    public DateTime Date { get; set; }
    public decimal IncomingTotal { get; set; }
    public decimal OutgoingTotal { get; set; }
    public int IncomingCount { get; set; }
    public int OutgoingCount { get; set; }
}

// Stock levels
public class StockLevelDto
{
    public string ProductName { get; set; }
    public string UnitShortName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public bool IsLow => CurrentStock <= MinStock;
}

// Stock movement history
public class StockHistoryDto
{
    public DateTime Date { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } // "Incoming", "Outgoing"
}

// Finance income vs expense
public class IncomeExpenseDto
{
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Income - Expense;
}

// Top debtors
public class TopDebtorDto
{
    public string CounterpartyName { get; set; }
    public CounterpartyType Type { get; set; }
    public decimal DebtAmount { get; set; }
}

// Shift efficiency
public class ShiftEfficiencyDto
{
    public string ShiftName { get; set; }
    public DateTime Date { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public decimal EfficiencyPercent => PlannedQty > 0
        ? Math.Round(ActualQty / PlannedQty * 100, 1) : 0;
}

// Attendance heatmap
public class AttendanceHeatmapDto
{
    public string WorkerName { get; set; }
    public DateTime Date { get; set; }
    public int HoursWorked { get; set; }
    public bool WasPresent { get; set; }
}

// Product distribution
public class ProductDistributionDto
{
    public string ProductName { get; set; }
    public ProductType Type { get; set; }
    public decimal TotalStock { get; set; }
    public decimal Percentage { get; set; }
}
```

---

### 15.3 Analytics Service Interface

```csharp
// WMS.Application/Interfaces/IAnalyticsService.cs
public interface IAnalyticsService
{
    Task<DashboardSummaryDto> GetDashboardSummary(int tenantId);
    Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int tenantId, int days);
    Task<List<WasteByStageDto>> GetWasteByStage(int tenantId, int days);
    Task<List<DailyTransferDto>> GetDailyTransfers(int tenantId, int days);
    Task<List<StockLevelDto>> GetStockLevels(int tenantId, int? warehouseId);
    Task<List<StockHistoryDto>> GetStockHistory(int tenantId, int days);
    Task<List<IncomeExpenseDto>> GetIncomeExpense(int tenantId, int days);
    Task<List<TopDebtorDto>> GetTopDebtors(int tenantId, int top);
    Task<List<ShiftEfficiencyDto>> GetShiftEfficiency(int tenantId, int days);
    Task<List<AttendanceHeatmapDto>> GetAttendanceHeatmap(int tenantId, int days);
    Task<List<ProductDistributionDto>> GetProductDistribution(int tenantId);
}
```

---

### 15.4 Analytics Service Implementation

```csharp
// WMS.Infrastructure/Services/AnalyticsService.cs
public class AnalyticsService : IAnalyticsService
{
    private readonly WmsDbContext _db;
    public AnalyticsService(WmsDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> GetDashboardSummary(int tenantId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var totalStock = await _db.WarehouseStocks
            .Where(s => s.TenantId == tenantId)
            .SumAsync(s => s.Quantity);

        var activeOrders = await _db.ProductionOrders
            .Where(o => o.TenantId == tenantId && o.Status == ProductionOrderStatus.InProgress)
            .CountAsync();

        var pendingTransfers = await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.Status == TransferStatus.Pending)
            .CountAsync();

        var monthRevenue = await _db.Transactions
            .Where(t => t.TenantId == tenantId
                && t.Type == TransactionType.Income
                && t.Date >= monthStart)
            .SumAsync(t => t.Amount);

        var lastMonthRevenue = await _db.Transactions
            .Where(t => t.TenantId == tenantId
                && t.Type == TransactionType.Income
                && t.Date >= lastMonthStart && t.Date < monthStart)
            .SumAsync(t => t.Amount);

        var revenueChange = lastMonthRevenue > 0
            ? Math.Round((monthRevenue - lastMonthRevenue) / lastMonthRevenue * 100, 1)
            : 0;

        var lowStockCount = await _db.Products
            .Where(p => p.TenantId == tenantId)
            .Join(_db.WarehouseStocks.Where(s => s.TenantId == tenantId),
                p => p.Id, s => s.ProductId,
                (p, s) => new { p.MinStock, s.Quantity })
            .Where(x => x.Quantity <= x.MinStock)
            .CountAsync();

        var totalDebt = await _db.Debts
            .Where(d => d.TenantId == tenantId)
            .SumAsync(d => d.Amount);

        // Efficiency: last 7 days
        var from = now.AddDays(-7);
        var plans = await _db.ShiftPlans
            .Where(p => p.TenantId == tenantId && p.Date >= from)
            .SumAsync(p => p.PlannedQuantity);
        var actuals = await _db.ShiftActuals
            .Where(a => a.TenantId == tenantId && a.Date >= from)
            .SumAsync(a => a.ActualQuantity);
        var efficiency = plans > 0 ? Math.Round(actuals / plans * 100, 1) : 0;

        return new DashboardSummaryDto
        {
            TotalStockKg = totalStock,
            ActiveProductionOrders = activeOrders,
            PendingTransfers = pendingTransfers,
            MonthlyRevenue = monthRevenue,
            RevenueChangePercent = revenueChange,
            LowStockProductCount = lowStockCount,
            OverallEfficiencyPercent = efficiency,
            TotalDebt = totalDebt
        };
    }

    public async Task<List<PlanVsActualDto>> GetProductionPlanVsActual(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var plans = await _db.ShiftPlans
            .Include(p => p.Product)
            .Where(p => p.TenantId == tenantId && p.Date >= from)
            .ToListAsync();

        var actuals = await _db.ShiftActuals
            .Include(a => a.Product)
            .Where(a => a.TenantId == tenantId && a.Date >= from)
            .ToListAsync();

        return plans
            .GroupBy(p => new { p.Date.Date, p.Product.Name })
            .Select(g => new PlanVsActualDto
            {
                Date = g.Key.Date,
                ProductName = g.Key.Name,
                Planned = g.Sum(p => p.PlannedQuantity),
                Actual = actuals
                    .Where(a => a.Date.Date == g.Key.Date && a.Product.Name == g.Key.Name)
                    .Sum(a => a.ActualQuantity)
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<List<DailyTransferDto>> GetDailyTransfers(int tenantId, int days)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var transfers = await _db.Transfers
            .Where(t => t.TenantId == tenantId
                && t.Status == TransferStatus.Confirmed
                && t.CreatedAt >= from)
            .Include(t => t.Items)
            .ToListAsync();

        return transfers
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailyTransferDto
            {
                Date = g.Key,
                IncomingTotal = g.Where(t => t.Type == TransferType.Incoming)
                    .SelectMany(t => t.Items).Sum(i => i.TotalPrice),
                OutgoingTotal = g.Where(t => t.Type == TransferType.Outgoing)
                    .SelectMany(t => t.Items).Sum(i => i.TotalPrice),
                IncomingCount = g.Count(t => t.Type == TransferType.Incoming),
                OutgoingCount = g.Count(t => t.Type == TransferType.Outgoing),
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    // Implement remaining methods similarly...
    // GetWasteByStage, GetStockLevels, GetStockHistory,
    // GetIncomeExpense, GetTopDebtors, GetShiftEfficiency,
    // GetAttendanceHeatmap, GetProductDistribution
    // Follow the same pattern: filter by tenantId + date range,
    // group and aggregate with LINQ, return typed DTOs
}
```

---

### 15.5 Register Analytics Service

```csharp
// Program.cs — add to DI
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
```

---

### 15.6 Frontend: Analytics Service

```typescript
// core/services/analytics.service.ts
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private api = inject(ApiService);

  getSummary() {
    return this.api.get<DashboardSummaryDto>('analytics/summary');
  }

  getProductionPlanVsActual(days = 7) {
    return this.api.get<PlanVsActualDto[]>('analytics/production/plan-vs-actual', { days });
  }

  getDailyTransfers(days = 7) {
    return this.api.get<DailyTransferDto[]>('analytics/transfers/daily', { days });
  }

  getStockLevels(warehouseId?: number) {
    return this.api.get<StockLevelDto[]>('analytics/warehouse/stock-levels',
      warehouseId ? { warehouseId } : {});
  }

  getIncomeExpense(days = 30) {
    return this.api.get<IncomeExpenseDto[]>('analytics/finance/income-expense', { days });
  }

  getShiftEfficiency(days = 7) {
    return this.api.get<ShiftEfficiencyDto[]>('analytics/kpi/shift-efficiency', { days });
  }

  getProductDistribution() {
    return this.api.get<ProductDistributionDto[]>('analytics/products/distribution');
  }
}
```

---

### 15.7 Frontend: TypeScript Interfaces for Charts

```typescript
// core/models/analytics.model.ts

export interface DashboardSummaryDto {
  totalStockKg: number;
  activeProductionOrders: number;
  pendingTransfers: number;
  monthlyRevenue: number;
  revenueChangePercent: number;
  lowStockProductCount: number;
  overallEfficiencyPercent: number;
  totalDebt: number;
}

export interface PlanVsActualDto {
  date: string;
  productName: string;
  planned: number;
  actual: number;
  efficiencyPercent: number;
}

export interface DailyTransferDto {
  date: string;
  incomingTotal: number;
  outgoingTotal: number;
  incomingCount: number;
  outgoingCount: number;
}

export interface StockLevelDto {
  productName: string;
  unitShortName: string;
  currentStock: number;
  minStock: number;
  isLow: boolean;
}

export interface IncomeExpenseDto {
  date: string;
  income: number;
  expense: number;
  net: number;
}

export interface ShiftEfficiencyDto {
  shiftName: string;
  date: string;
  plannedQty: number;
  actualQty: number;
  efficiencyPercent: number;
}

export interface ProductDistributionDto {
  productName: string;
  type: string;
  totalStock: number;
  percentage: number;
}
```

---

### 15.8 Implementation Order for Analytics

Add to Phase 2 (after core backend):
1. Create all Analytics DTOs
2. Implement `IAnalyticsService` interface
3. Implement `AnalyticsService` (all methods)
4. Register in DI
5. Create `AnalyticsController`
6. Test all endpoints in Swagger

Add to Phase 4 (already included above in implementation order).

---

## 17. NOTES FOR AGENT

- Always use Angular signals (`signal()`, `computed()`, `effect()`) — no BehaviorSubject
- Always use `inject()` instead of constructor injection
- All Angular components must be `standalone: true`
- Use `ChangeDetectionStrategy.OnPush` on all components
- Backend: always filter by `TenantId` from JWT claims — never trust TenantId from request body
- Use soft delete (`IsDeleted = true`) — never hard delete
- All money values: `decimal` in C#, never `float` or `double`
- Dates: always UTC in backend, convert to local in frontend
- API responses: always use `ApiResponse<T>` wrapper
- Pagination: all list endpoints support `?page=1&pageSize=20`
- `TransferItem.TotalPrice` is computed — add `[NotMapped]` attribute, do NOT map to DB
- `Debt` entity has `UpdatedAt` — remove it, `BaseEntity` already has it
- `QcCheck` must have either `StageExecutionId` OR `TransferId` — validate in service
- Module seed data uses fixed IDs 1–9 — never re-seed
- On production order completion: auto-create `Transfer` of type `ProductionOutput`
- All chart components: spread `APEX_DEFAULTS` as base config, override per chart type
- Transloco: load translations lazily per module using `provideTranslocoScope`
- Add `[NotMapped]` to all computed properties (TotalPrice, EfficiencyPercent, WastePercent)
- Portal JWT is separate from main JWT — use different claim type to distinguish
- Portal is read-only in Phase 1 — no POST/PUT endpoints except login
- `NotificationService` must be used everywhere — never call `MessageService` directly
