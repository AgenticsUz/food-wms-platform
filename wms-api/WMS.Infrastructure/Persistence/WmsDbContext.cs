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
                var method = typeof(WmsDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }

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
            new Module { Id = 9, Name = "Quality Control", Code = "QUALITY", OrderNumber = 9, CreatedAt = seedDate, UpdatedAt = seedDate }
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
