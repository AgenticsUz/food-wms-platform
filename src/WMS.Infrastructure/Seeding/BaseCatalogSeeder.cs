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
    /// <summary>
    /// Katalog yozuvi. <paramref name="InPlans"/> = <see langword="false"/> — feature hech bir
    /// planga kirmaydi va sukut bo'yicha o'chiq: uni Console operatori tenantga oshkora yoqadi.
    /// </summary>
    private sealed record CatalogEntry(string Code, string Name, string? Module, int Sort, bool InPlans = true);

    private static readonly CatalogEntry[] FeatureCatalog =
    [
        new(FeatureCodes.WarehouseStock, "Stock overview", ModuleCodes.WarehouseRaw, 10),
        new(FeatureCodes.WarehouseLocations, "Storage locations", ModuleCodes.WarehouseRaw, 20),
        new(FeatureCodes.WarehouseBatches, "Batches / lots", ModuleCodes.WarehouseRaw, 30),
        new(FeatureCodes.TransfersIncoming, "Incoming transfers", ModuleCodes.Transfers, 40),
        new(FeatureCodes.TransfersOutgoing, "Outgoing transfers", ModuleCodes.Transfers, 50),
        new(FeatureCodes.TransfersInternal, "Internal transfers", ModuleCodes.Transfers, 60),
        new(FeatureCodes.TransfersReturn, "Returns", ModuleCodes.Transfers, 70),
        new(FeatureCodes.ProductionStages, "Production stages", ModuleCodes.Production, 80),
        new(FeatureCodes.ProductionRecipes, "Recipes", ModuleCodes.Production, 90),
        new(FeatureCodes.ProductionOrders, "Production orders", ModuleCodes.Production, 100),
        new(FeatureCodes.QcParameters, "QC parameters", ModuleCodes.Quality, 110),
        new(FeatureCodes.QcChecks, "QC checks", ModuleCodes.Quality, 120),
        new(FeatureCodes.FinanceTransactions, "Transactions", ModuleCodes.Finance, 130),
        new(FeatureCodes.FinanceDebts, "Debts", ModuleCodes.Finance, 140),
        new(FeatureCodes.FinancePayments, "Payments", ModuleCodes.Finance, 150),
        new(FeatureCodes.CounterpartiesSuppliers, "Suppliers", ModuleCodes.Suppliers, 155),
        new(FeatureCodes.CounterpartiesClients, "Clients", ModuleCodes.Clients, 158),
        new(FeatureCodes.KpiShifts, "Shifts", ModuleCodes.Kpi, 170),
        new(FeatureCodes.KpiPlans, "Shift plans & actuals", ModuleCodes.Kpi, 180),
        new(FeatureCodes.KpiAttendance, "Attendance", ModuleCodes.Kpi, 190),
        new(FeatureCodes.KpiEfficiency, "Efficiency reports", ModuleCodes.Kpi, 200),
        new(FeatureCodes.DeliveryFleet, "Vehicles & drivers", ModuleCodes.Delivery, 210),
        new(FeatureCodes.DeliveryRoutes, "Delivery routes", ModuleCodes.Delivery, 220),
        new(FeatureCodes.AgentsCommissions, "Agent commissions", ModuleCodes.Agents, 230),
        new(FeatureCodes.AnalyticsAdvanced, "Advanced analytics", null, 240),
        new(FeatureCodes.ExportExcel, "Excel export", null, 250),
        new(FeatureCodes.ExportPdf, "PDF documents", null, 260),
        new(FeatureCodes.ImportExcel, "Excel import", null, 270),

        // ⚠️ Planlarga KIRMAYDI va sukuti o'chiq — sababi `FeatureCodes.AiChat` izohida.
        new(FeatureCodes.AiChat, "AI assistant", null, 280, InPlans: false),
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

        foreach (CatalogEntry entry in FeatureCatalog.Where(f => !known.Contains(f.Code)))
        {
            _db.Features.Add(new Feature
            {
                Code = entry.Code,
                Name = entry.Name,
                ModuleCode = entry.Module,

                // Plandan tashqaridagi feature plansiz tenantda ham yoqilmasin: sukut
                // «yoqiq» bo'lsa u eshikdan emas, derazadan kirib kelardi.
                DefaultEnabled = entry.InPlans,
                SortOrder = entry.Sort,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (await _db.Plans.AnyAsync(cancellationToken))
        {
            return;
        }

        string all = FeaturesFor(null);
        // AI kvotasi planda bor, lekin `ai.chat` feature'i yo'q: kvota — AI YOQILGANDA
        // amal qiladigan chegara, yoqish esa alohida qaror (Console override'i).
        _db.Plans.AddRange(
            new Plan { Name = "Trial", Code = "trial", Price = 0, IsDefault = true, TrialDays = 14, FeatureCodes = all, MaxUsers = 3, MaxWarehouses = 2, MaxTransfersPerMonth = 200, MaxAiRequestsPerMonth = 100 },
            new Plan { Name = "Basic", Code = "basic", Price = 1_200_000, FeatureCodes = FeaturesFor(BasicModules), MaxUsers = 5, MaxWarehouses = 3, MaxTransfersPerMonth = 1000, MaxAiRequestsPerMonth = 500 },
            new Plan { Name = "Pro", Code = "pro", Price = 2_900_000, FeatureCodes = FeaturesFor(ProModules), MaxUsers = 25, MaxWarehouses = 10, MaxTransfersPerMonth = 10_000, MaxAiRequestsPerMonth = 3_000 },
            new Plan { Name = "Enterprise", Code = "enterprise", Price = 5_900_000, FeatureCodes = all, MaxUsers = 200, MaxWarehouses = 50, MaxTransfersPerMonth = 100_000, MaxAiRequestsPerMonth = 20_000 });

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Modullarga tegishli (va kesishuvchi) feature'lar; <see langword="null"/> — hammasi.</summary>
    private static string FeaturesFor(string[]? modules) =>
        PlanModules.Join(FeatureCatalog
            .Where(f => f.InPlans)
            .Where(f => modules is null || f.Module is null || modules.Contains(f.Module, StringComparer.Ordinal))
            .Select(f => f.Code));
}
