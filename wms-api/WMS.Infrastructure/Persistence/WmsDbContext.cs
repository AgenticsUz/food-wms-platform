using Microsoft.EntityFrameworkCore;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence;

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
    public DbSet<Plan> Plans => Set<Plan>();

    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();

    // Counterparties
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    // Agents
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<CommissionRecord> CommissionRecords => Set<CommissionRecord>();

    // Warehouse
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferItem> TransferItems => Set<TransferItem>();

    // Delivery
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryStop> DeliveryStops => Set<DeliveryStop>();

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

    // Permissions
    public DbSet<Permission> Permissions => Set<Permission>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);

        // Global soft-delete filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(WmsDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        // Tenant → Plan (control plane) — nullable, detach the plan on delete
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Plan)
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.SetNull);

        // Transfer: multiple FK to Warehouse
        modelBuilder.Entity<Transfer>()
            .HasOne(t => t.FromWarehouse)
            .WithMany()
            .HasForeignKey(t => t.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transfer>()
            .HasOne(t => t.ToWarehouse)
            .WithMany()
            .HasForeignKey(t => t.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Agent relationships
        modelBuilder.Entity<Transfer>()
            .HasOne(t => t.Agent)
            .WithMany()
            .HasForeignKey(t => t.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Counterparty>()
            .HasOne(c => c.Agent)
            .WithMany()
            .HasForeignKey(c => c.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Agent>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CommissionRecord>()
            .HasOne(cr => cr.Agent)
            .WithMany()
            .HasForeignKey(cr => cr.AgentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CommissionRecord>()
            .HasOne(cr => cr.Transfer)
            .WithMany()
            .HasForeignKey(cr => cr.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        // Delivery: multiple/optional FK — restrict to avoid cascade issues
        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.Vehicle)
            .WithMany()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.Driver)
            .WithMany()
            .HasForeignKey(d => d.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>()
            .HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryStop>()
            .HasOne(s => s.Delivery)
            .WithMany(d => d.Stops)
            .HasForeignKey(s => s.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeliveryStop>()
            .HasOne(s => s.Counterparty)
            .WithMany()
            .HasForeignKey(s => s.CounterpartyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeliveryStop>()
            .HasOne(s => s.Transfer)
            .WithMany()
            .HasForeignKey(s => s.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Delivery>().HasIndex(d => new { d.TenantId, d.Status });

        // Audit log — tenant + vaqt bo'yicha tez filtrlash uchun indeks
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.TenantId, a.CreatedAt });

        // ── Performance indekslar — barcha so'rovlar TenantId bilan filtrlanadi ──
        modelBuilder.Entity<Transfer>().HasIndex(t => new { t.TenantId, t.Status });
        modelBuilder.Entity<Transfer>().HasIndex(t => new { t.TenantId, t.ConfirmedAt });
        modelBuilder.Entity<WarehouseStock>().HasIndex(s => new { s.TenantId, s.WarehouseId, s.ProductId });
        modelBuilder.Entity<Batch>().HasIndex(b => new { b.TenantId, b.ProductId });
        modelBuilder.Entity<Batch>().HasIndex(b => b.ExpiryDate);
        modelBuilder.Entity<Transaction>().HasIndex(t => new { t.TenantId, t.Date });
        modelBuilder.Entity<Product>().HasIndex(p => p.TenantId);
        modelBuilder.Entity<Counterparty>().HasIndex(c => c.TenantId);
        modelBuilder.Entity<ShiftPlan>().HasIndex(p => new { p.TenantId, p.Date });
        modelBuilder.Entity<ShiftActual>().HasIndex(a => new { a.TenantId, a.Date });
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.TenantId, n.UserId, n.IsRead });
        modelBuilder.Entity<CommissionRecord>().HasIndex(c => new { c.TenantId, c.AgentId });

        // ── Noyoblik kafolatlari (DB darajasida) ──
        // Slug login uchun kalit: ikkita tirik tenant bir xil slug bilan bo'lsa, mijoz
        // noto'g'ri tenantga kiradi. Servis darajasidagi AnyAsync tekshiruvi parallel
        // so'rovlarda yetarli emas — shuning uchun qisman (soft-delete'ni hisobga oluvchi)
        // unique indeks qo'yiladi.
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Slug).IsUnique().HasFilter("\"IsDeleted\" = 0");
        modelBuilder.Entity<Plan>()
            .HasIndex(p => p.Code).IsUnique().HasFilter("\"IsDeleted\" = 0");

        // Seed Modules (static dates required by EF Core to avoid PendingModelChangesWarning)
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Module>().HasData(
            new Module { Id = 1, Name = "Raw Material Warehouse", Code = "WAREHOUSE_RAW", OrderNumber = 1, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 2, Name = "Production", Code = "PRODUCTION", OrderNumber = 2, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 3, Name = "Finished Goods Warehouse", Code = "WAREHOUSE_FINISHED", OrderNumber = 3, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 4, Name = "Transfer System", Code = "TRANSFERS", OrderNumber = 4, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 5, Name = "Finance", Code = "FINANCE", OrderNumber = 5, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 6, Name = "KPI & Shifts", Code = "KPI", OrderNumber = 6, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 7, Name = "Suppliers", Code = "SUPPLIERS", OrderNumber = 7, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 8, Name = "Clients", Code = "CLIENTS", OrderNumber = 8, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 9, Name = "Quality Control", Code = "QUALITY", OrderNumber = 9, CreatedAt = seedDate, UpdatedAt = seedDate },
            // Agents/Delivery had permissions but no Module row, so their endpoints could
            // not be gated by plan. Existing tenants are backfilled as enabled by
            // DataInitializer.BackfillMissingTenantModulesAsync (no behaviour change for them).
            new Module { Id = 10, Name = "Agents", Code = "AGENTS", OrderNumber = 10, CreatedAt = seedDate, UpdatedAt = seedDate },
            new Module { Id = 11, Name = "Delivery", Code = "DELIVERY", OrderNumber = 11, CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        // Seed Permissions
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Code = "dashboard.view", Name = "View Dashboard", Module = "DASHBOARD", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 2, Code = "warehouse.view", Name = "View Warehouse", Module = "WAREHOUSE", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 3, Code = "warehouse.manage", Name = "Manage Warehouse", Module = "WAREHOUSE", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 4, Code = "transfers.view", Name = "View Transfers", Module = "TRANSFERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 5, Code = "transfers.create", Name = "Create Transfers", Module = "TRANSFERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 6, Code = "transfers.confirm", Name = "Confirm Transfers", Module = "TRANSFERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 7, Code = "transfers.reject", Name = "Reject Transfers", Module = "TRANSFERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 8, Code = "production.view", Name = "View Production", Module = "PRODUCTION", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 9, Code = "production.manage", Name = "Manage Production", Module = "PRODUCTION", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 10, Code = "finance.view", Name = "View Finance", Module = "FINANCE", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 11, Code = "finance.manage", Name = "Manage Finance", Module = "FINANCE", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 12, Code = "kpi.view", Name = "View KPI", Module = "KPI", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 13, Code = "kpi.manage", Name = "Manage KPI", Module = "KPI", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 14, Code = "partners.view", Name = "View Partners", Module = "PARTNERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 15, Code = "partners.manage", Name = "Manage Partners", Module = "PARTNERS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 16, Code = "products.view", Name = "View Products", Module = "PRODUCTS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 17, Code = "products.manage", Name = "Manage Products", Module = "PRODUCTS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 18, Code = "settings.users", Name = "Manage Users", Module = "SETTINGS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 19, Code = "settings.roles", Name = "Manage Roles", Module = "SETTINGS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 20, Code = "settings.modules", Name = "Manage Modules", Module = "SETTINGS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 21, Code = "quality.view", Name = "View Quality", Module = "QUALITY", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 22, Code = "quality.manage", Name = "Manage Quality", Module = "QUALITY", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 23, Code = "agents.view", Name = "View Agents", Module = "AGENTS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 24, Code = "agents.manage", Name = "Manage Agents", Module = "AGENTS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 25, Code = "audit.view", Name = "View Audit Log", Module = "SETTINGS", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 26, Code = "delivery.view", Name = "View Delivery", Module = "DELIVERY", CreatedAt = seedDate, UpdatedAt = seedDate },
            new Permission { Id = 27, Code = "delivery.manage", Name = "Manage Delivery", Module = "DELIVERY", CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion<double>();
                }
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
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
