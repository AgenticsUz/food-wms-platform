using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Seeding;

/// <summary>
/// Platforma darajasidagi bazaviy katalog: feature'lar va sukut planlari. Har migratsiyadan keyin,
/// IDEMPOTENT (<c>migrate</c> rejimi).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davridagi <c>DataInitializer.SeedFeaturesAsync</c>/<c>SeedPlansAsync</c> dan. Farqi (D6):
/// plan endi MODUL bermaydi — modul Identity obunasida; plan feature'lar to'plamini beradi.
/// Asosiy/Pro planlarning feature to'plami eski modul ro'yxatidan hisoblanadi, ya'ni tijorat
/// ma'nosi o'zgarmadi.
/// </para>
/// <para>
/// Mavjud feature va planlar QAYTA YOZILMAYDI: Console operatori ularni tahrirlagan bo'lishi
/// mumkin. Faqat yetishmagan feature qo'shiladi; planlar bo'sh bazadagina yaratiladi.
/// </para>
/// </remarks>
public sealed class BaseCatalogSeeder
{
    private static readonly (string Code, string Name, string? Module, int Sort)[] FeatureCatalog =
    [
        (FeatureCodes.WarehouseStock, "Stock overview", ModuleCodes.WarehouseRaw, 10),
        (FeatureCodes.WarehouseLocations, "Storage locations", ModuleCodes.WarehouseRaw, 20),
        (FeatureCodes.WarehouseBatches, "Batches / lots", ModuleCodes.WarehouseRaw, 30),
        (FeatureCodes.TransfersIncoming, "Incoming transfers", ModuleCodes.Transfers, 40),
        (FeatureCodes.TransfersOutgoing, "Outgoing transfers", ModuleCodes.Transfers, 50),
        (FeatureCodes.TransfersInternal, "Internal transfers", ModuleCodes.Transfers, 60),
        (FeatureCodes.TransfersReturn, "Returns", ModuleCodes.Transfers, 70),
        (FeatureCodes.ProductionStages, "Production stages", ModuleCodes.Production, 80),
        (FeatureCodes.ProductionRecipes, "Recipes", ModuleCodes.Production, 90),
        (FeatureCodes.ProductionOrders, "Production orders", ModuleCodes.Production, 100),
        (FeatureCodes.QcParameters, "QC parameters", ModuleCodes.Quality, 110),
        (FeatureCodes.QcChecks, "QC checks", ModuleCodes.Quality, 120),
        (FeatureCodes.FinanceTransactions, "Transactions", ModuleCodes.Finance, 130),
        (FeatureCodes.FinanceDebts, "Debts", ModuleCodes.Finance, 140),
        (FeatureCodes.FinancePayments, "Payments", ModuleCodes.Finance, 150),
        (FeatureCodes.CounterpartiesSuppliers, "Suppliers", ModuleCodes.Suppliers, 155),
        (FeatureCodes.CounterpartiesClients, "Clients", ModuleCodes.Clients, 158),
        (FeatureCodes.KpiShifts, "Shifts", ModuleCodes.Kpi, 170),
        (FeatureCodes.KpiPlans, "Shift plans & actuals", ModuleCodes.Kpi, 180),
        (FeatureCodes.KpiAttendance, "Attendance", ModuleCodes.Kpi, 190),
        (FeatureCodes.KpiEfficiency, "Efficiency reports", ModuleCodes.Kpi, 200),
        (FeatureCodes.DeliveryFleet, "Vehicles & drivers", ModuleCodes.Delivery, 210),
        (FeatureCodes.DeliveryRoutes, "Delivery routes", ModuleCodes.Delivery, 220),
        (FeatureCodes.AgentsCommissions, "Agent commissions", ModuleCodes.Agents, 230),
        (FeatureCodes.AnalyticsAdvanced, "Advanced analytics", null, 240),
        (FeatureCodes.ExportExcel, "Excel export", null, 250),
        (FeatureCodes.ExportPdf, "PDF documents", null, 260),
        (FeatureCodes.ImportExcel, "Excel import", null, 270),
    ];

    private static readonly string[] BasicModules =
        [ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished, ModuleCodes.Transfers, ModuleCodes.Suppliers, ModuleCodes.Clients];

    private static readonly string[] ProModules =
        [.. BasicModules, ModuleCodes.Production, ModuleCodes.Finance, ModuleCodes.Quality];

    private readonly WmsDbContext _db;

    public BaseCatalogSeeder(WmsDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        HashSet<string> known = new(await _db.Features.Select(f => f.Code).ToListAsync(cancellationToken), StringComparer.OrdinalIgnoreCase);

        foreach ((string code, string name, string? module, int sort) in FeatureCatalog.Where(f => !known.Contains(f.Code)))
        {
            _db.Features.Add(new Feature { Code = code, Name = name, ModuleCode = module, DefaultEnabled = true, SortOrder = sort });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (await _db.Plans.AnyAsync(cancellationToken))
        {
            return;
        }

        string all = FeaturesFor(null);
        _db.Plans.AddRange(
            new Plan { Name = "Trial", Code = "trial", Price = 0, IsDefault = true, TrialDays = 14, FeatureCodes = all, MaxUsers = 3, MaxWarehouses = 2, MaxTransfersPerMonth = 200 },
            new Plan { Name = "Basic", Code = "basic", Price = 1_200_000, FeatureCodes = FeaturesFor(BasicModules), MaxUsers = 5, MaxWarehouses = 3, MaxTransfersPerMonth = 1000 },
            new Plan { Name = "Pro", Code = "pro", Price = 2_900_000, FeatureCodes = FeaturesFor(ProModules), MaxUsers = 25, MaxWarehouses = 10, MaxTransfersPerMonth = 10_000 },
            new Plan { Name = "Enterprise", Code = "enterprise", Price = 5_900_000, FeatureCodes = all, MaxUsers = 200, MaxWarehouses = 50, MaxTransfersPerMonth = 100_000 });

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Modullarga tegishli (va kesishuvchi) feature'lar; <see langword="null"/> — hammasi.</summary>
    private static string FeaturesFor(string[]? modules) =>
        PlanModules.Join(FeatureCatalog
            .Where(f => modules is null || f.Module is null || modules.Contains(f.Module, StringComparer.Ordinal))
            .Select(f => f.Code));
}
