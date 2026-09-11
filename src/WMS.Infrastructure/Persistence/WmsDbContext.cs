using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Platform.Infrastructure.Persistence;
using Platform.Infrastructure.Tenancy;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence.Naming;
using WMS.Infrastructure.Persistence.Rls;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// WMS'ning YAGONA <see cref="DbContext"/>i (sxema <c>wms</c>).
/// </summary>
/// <remarks>
/// <para>
/// Tenant izolyatsiyasi ikki qatlamli (Wash naqshi): 1-qatlam — shu yerdagi global filtr va
/// <see cref="StampEntries"/> tekshiruvi; 2-qatlam — Postgres RLS
/// (<see cref="WmsMigrationsSqlGenerator"/> + <see cref="TenantConnectionInterceptor"/>).
/// SQLite davridagi ~245 ta qo'lda yozilgan <c>TenantId == tenantId</c> sharti shu ikkisiga
/// almashdi (D4) — servis tenantni parametr sifatida OLMAYDI.
/// </para>
/// <para>
/// ⚠️ <c>DbSet</c> ro'yxati integratorniki (HOLAT §5): modul agenti yangi entity qo'shsa
/// hisobotida aytadi, bu faylga o'zi tegmaydi. Model konfiguratsiyasi esa har modulning o'z
/// faylida (<c>Configurations/*Configuration.cs</c>).
/// </para>
/// </remarks>
public sealed class WmsDbContext : DbContext
{
    public const string DefaultSchema = "wms";
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    private const string TenantIdPropertyName = nameof(ITenantEntity.TenantId);
    private const string SoftDeletePropertyName = nameof(BaseEntity.IsDeleted);

    /// <summary>
    /// <c>xmin</c> optimistik tokeni turadigan jadvallar (D13). Hammasi JOYIDA o'zgartiriladigan
    /// balans yoki holat: parallel ikki tasdiq SQLite'da ikkalasi ham o'tardi.
    /// </summary>
    private static readonly Type[] ConcurrencyGuarded =
    [
        typeof(WarehouseStock), typeof(Batch), typeof(Debt),
        typeof(Transfer), typeof(ProductionOrder), typeof(StageExecution),
    ];

    /// <summary>
    /// Miqdor ustunlari <c>numeric(18,3)</c> (kg, litr — uch xona); qolgan <c>decimal</c> pul,
    /// <c>numeric(18,2)</c> (D13). Nom bo'yicha: SQLite davrida hammasi <c>double</c> edi va
    /// qaysi biri pul ekani faqat nomdan ko'rinadi.
    /// </summary>
    private static readonly string[] QuantityNameMarkers = ["Quantity", "Qty"];
    private static readonly HashSet<string> QuantityNames = new(StringComparer.Ordinal)
    {
        nameof(Product.MinStock), nameof(Vehicle.Capacity), nameof(QcParameter.MinValue), nameof(QcParameter.MaxValue),
    };

    private static readonly MethodInfo EfPropertyMethod =
        typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Static | BindingFlags.Public)!;

    private readonly ICurrentTenant _currentTenant;

    public WmsDbContext(DbContextOptions<WmsDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    /// <summary>Joriy tenant — global filtr shu xossadan HAR so'rovda o'qiydi.</summary>
    public Guid? CurrentTenantId => _currentTenant.TenantId;

    // ── Platforma jadvallari (RLS yo'q): Identity nusxasi + tijorat qatlami ──
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Feature> Features => Set<Feature>();

    // Telegram deep-link tokeni va navbat — tenant kontekstisiz o'qiladi (polling, yuboruvchi).
    public DbSet<TelegramLinkToken> TelegramLinkTokens => Set<TelegramLinkToken>();
    public DbSet<TelegramOutbox> TelegramOutboxes => Set<TelegramOutbox>();
    public DbSet<TelegramChatState> TelegramChatStates => Set<TelegramChatState>();

    // ── Tenant jadvallari (RLS) ──
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TelegramLink> TelegramLinks => Set<TelegramLink>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferItem> TransferItems => Set<TransferItem>();

    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<CommissionRecord> CommissionRecords => Set<CommissionRecord>();

    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<ProductionStage> ProductionStages => Set<ProductionStage>();
    public DbSet<ProductionRecipe> ProductionRecipes => Set<ProductionRecipe>();
    public DbSet<RecipeStage> RecipeStages => Set<RecipeStage>();
    public DbSet<RecipeStageItem> RecipeStageItems => Set<RecipeStageItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<StageExecution> StageExecutions => Set<StageExecution>();

    public DbSet<QcParameter> QcParameters => Set<QcParameter>();
    public DbSet<QcCheck> QcChecks => Set<QcCheck>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryStop> DeliveryStops => Set<DeliveryStop>();

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftPlan> ShiftPlans => Set<ShiftPlan>();
    public DbSet<ShiftActual> ShiftActuals => Set<ShiftActual>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamptz")
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);

        // ⚠️ Kaskad o'chirish SUKUT BO'YICHA YO'Q. Postgres (SQL Server'dan farqli) ko'p kaskad
        // yo'lini qabul qiladi, ya'ni EF sukuti jimgina ishlardi: mahsulotni o'chirish uning
        // partiyalari, zaxirasi va transfer qatorlarini ham olib ketardi. Bola to'plamlari
        // (transfer qatorlari, retsept bosqichlari...) kaskadni o'z konfiguratsiyasida OSHKORA oladi.
        configurationBuilder.Conventions.Remove<CascadeDeleteConvention>();

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);

        ApplyTableNames(modelBuilder);
        ApplyTenantForeignKeys(modelBuilder);
        ApplyClientGeneratedKeyConvention(modelBuilder);
        ApplyNumericPrecision(modelBuilder);
        ApplyConcurrencyTokens(modelBuilder);
        ApplyGlobalFilterConventions(modelBuilder);

        // OXIRIDA: undan oldin berilgan nomlar ham snake_case'ga o'tsin (izohi SnakeCaseNaming'da).
        SnakeCaseNaming.Apply(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Jadval nomi — sinf nomi, BIRLIKDA (<c>wms.tenant</c>, <c>wms.audit_log</c>).
    /// </summary>
    /// <remarks>
    /// EF sukuti <c>DbSet</c> xossasining nomini oladi (<c>AuditLogs</c>), migrator va init SQL esa
    /// <c>wms.audit_log</c> ga REVOKE yozadi — ikki nom ayrilsa append-only kafolati jimgina yo'qolardi.
    /// </remarks>
    private static void ApplyTableNames(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is null && !entityType.IsOwned())
            {
                entityType.SetTableName(entityType.ClrType.Name);
            }
        }
    }

    /// <summary>
    /// Har tenant jadvali <c>tenant_id → tenant.id</c> FK oladi (navigatsiyasiz).
    /// </summary>
    /// <remarks>
    /// Tenant nusxasini JIT yozadi; FK bo'lmasa nusxasi yo'q tenant ostida yozilgan qator
    /// (buzuq token, qo'lda seed) jimgina «yetim» bo'lib qolardi va RLS uni hech kimga
    /// ko'rsatmasdi. Navigatsiyasi bor joylar (<c>TenantFeature.Tenant</c>) o'z konfiguratsiyasida.
    /// </remarks>
    private static void ApplyTenantForeignKeys(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entityType.BaseType is not null || !typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            IMutableProperty? tenantId = entityType.FindProperty(TenantIdPropertyName);
            if (tenantId is null || entityType.FindForeignKeys(tenantId).Any())
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .HasOne(typeof(Tenant))
                .WithMany()
                .HasForeignKey(TenantIdPropertyName)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    /// <summary>
    /// Kalit MIJOZ tomonda beriladi (<c>Guid.CreateVersion7()</c>), bazada emas (D3).
    /// </summary>
    /// <remarks>
    /// ⚠️ F4 dagi tuzoq (Wash, 44 jadvalda uyquda edi): EF ning <c>Guid</c> kaliti uchun sukuti
    /// <c>ValueGeneratedOnAdd</c> va u «kalit to'ldirilgan bo'lsa — qator bazada BOR» deb
    /// hisoblaydi. Kuzatilayotgan obyektning navigatsiyasiga qo'shilgan yangi bola <c>INSERT</c>
    /// o'rniga <c>UPDATE ... WHERE id = ...</c> olardi va «0 qator» bilan yiqilardi. WMS'da bu
    /// aynan transfer qatorlari va retsept bosqichlarida bo'lardi.
    /// </remarks>
    private static void ApplyClientGeneratedKeyConvention(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            IMutableProperty? id = entityType.FindProperty("Id");
            if (id is null || id.ClrType != typeof(Guid) || id.GetDefaultValueSql() is not null)
            {
                continue;
            }

            id.ValueGenerated = ValueGenerated.Never;
        }
    }

    private static void ApplyNumericPrecision(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                Type type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type != typeof(decimal))
                {
                    continue;
                }

                if (property.Name.EndsWith("Percent", StringComparison.Ordinal))
                {
                    property.SetPrecision(5);
                    property.SetScale(2);
                }
                else if (QuantityNames.Contains(property.Name)
                         || Array.Exists(QuantityNameMarkers, m => property.Name.Contains(m, StringComparison.Ordinal)))
                {
                    property.SetPrecision(18);
                    property.SetScale(3);
                }
            }
        }
    }

    /// <summary>
    /// <c>xmin</c> — Postgres tizim ustuni, jadvalga QO'SHILMAYDI; EF uni faqat
    /// <c>WHERE xmin = @old</c> sharti sifatida ishlatadi. Soya xossa: domen sinfiga infratuzilma
    /// tushunchasi kirmasin.
    /// </summary>
    /// <remarks>
    /// Parallel yozuvda ikkinchisi <c>DbUpdateConcurrencyException</c> oladi va API uni 409 qiladi
    /// (<c>ExceptionHandlingMiddleware</c>) — foydalanuvchi qayta yuklab takrorlaydi.
    /// </remarks>
    private static void ApplyConcurrencyTokens(ModelBuilder modelBuilder)
    {
        foreach (Type type in ConcurrencyGuarded)
        {
            modelBuilder.Entity(type)
                .Property<uint>("Version")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .IsRowVersion();
        }
    }

    /// <summary>
    /// <see cref="ITenantEntity"/> → tenant filtri + RLS annotatsiyasi; <see cref="BaseEntity"/> →
    /// «o'chirilmagan» filtri. Filtrlar NOMLI: bittasini alohida o'chirish mumkin
    /// (<c>IgnoreQueryFilters([AppQueryFilters.SoftDelete])</c>).
    /// </summary>
    private void ApplyGlobalFilterConventions(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is not null || entityType.IsOwned())
            {
                continue;
            }

            Type clrType = entityType.ClrType;
            bool isTenantScoped = typeof(ITenantEntity).IsAssignableFrom(clrType);
            bool isSoftDeletable = typeof(BaseEntity).IsAssignableFrom(clrType);

            if (isTenantScoped)
            {
                entityType.SetAnnotation(RlsAnnotations.Enabled, true);
            }

            if (!isTenantScoped && !isSoftDeletable)
            {
                continue;
            }

            ParameterExpression parameter = Expression.Parameter(clrType, "e");
            EntityTypeBuilder entityBuilder = modelBuilder.Entity(clrType);

            if (isTenantScoped)
            {
                Expression tenantColumn = Expression.Convert(
                    Expression.Call(EfPropertyMethod.MakeGenericMethod(typeof(Guid)), parameter, Expression.Constant(TenantIdPropertyName)),
                    typeof(Guid?));

                Expression currentTenant = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));

                entityBuilder.HasQueryFilter(
                    AppQueryFilters.Tenant,
                    Expression.Lambda(Expression.Equal(tenantColumn, currentTenant), parameter));
            }

            if (isSoftDeletable)
            {
                Expression notDeleted = Expression.Not(
                    Expression.Call(EfPropertyMethod.MakeGenericMethod(typeof(bool)), parameter, Expression.Constant(SoftDeletePropertyName)));

                entityBuilder.HasQueryFilter(AppQueryFilters.SoftDelete, Expression.Lambda(notDeleted, parameter));
            }
        }
    }

    /// <summary>SaveChanges'dan oldin: tenant majburiy, vaqt maydonlari.</summary>
    /// <remarks>
    /// ⚠️ <c>Remove()</c> yumshoq o'chirishga AYLANTIRILMAYDI (Wash'dan farqli). WMS servislari
    /// SQLite davridan beri <c>IsDeleted = true</c> ni o'zi qo'yadi va ba'zi joyda ATAYLAB
    /// qattiq o'chiradi (rol ruxsatlarini qayta yozish): hammasini yumshoqqa aylantirish
    /// noyob indekslarni (<c>role_id, permission_code</c>) o'chirilgan qator bilan to'qnashtirardi.
    /// </remarks>
    private void StampEntries()
    {
        DateTime now = DateTime.UtcNow;
        Guid? tenantId = _currentTenant.TenantId;

        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            if (entry.Entity is ITenantEntity)
            {
                ApplyTenant(entry, tenantId);
            }

            if (entry.Entity is BaseEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = now;
                    entity.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = now;
                }
            }
        }
    }

    /// <summary>Yangi yozuvga joriy tenantni yozadi, begona tenant yozuvini RAD ETADI (1-qatlam).</summary>
    private static void ApplyTenant(EntityEntry entry, Guid? tenantId)
    {
        PropertyEntry property = entry.Property(TenantIdPropertyName);
        Guid currentValue = property.CurrentValue is Guid value ? value : Guid.Empty;

        if (entry.State == EntityState.Added && currentValue == Guid.Empty)
        {
            if (tenantId is null)
            {
                throw new InvalidOperationException(
                    $"'{entry.Metadata.DisplayName()}' yozuvini saqlash uchun tenant konteksti o'rnatilmagan.");
            }

            property.CurrentValue = tenantId.Value;
            return;
        }

        if (tenantId is not null && currentValue != Guid.Empty && currentValue != tenantId.Value)
        {
            throw new InvalidOperationException(
                $"'{entry.Metadata.DisplayName()}' yozuvi boshqa tenantga tegishli " +
                $"(yozuv: {currentValue}, kontekst: {tenantId.Value}) — amal rad etildi.");
        }
    }
}
