namespace WMS.Application.Common;

/// <summary>
/// Feature codes seeded by <c>DataInitializer</c>. Every code here maps to a real page or
/// endpoint group — a feature nobody can switch on or off is just noise, so do not add one
/// before the surface it controls exists.
/// </summary>
public static class FeatureCodes
{
    // Warehouse
    public const string WarehouseStock = "warehouse.stock";
    public const string WarehouseLocations = "warehouse.locations";
    public const string WarehouseBatches = "warehouse.batches";

    // Transfers (checked per transfer type when one is created)
    public const string TransfersIncoming = "transfers.incoming";
    public const string TransfersOutgoing = "transfers.outgoing";
    public const string TransfersInternal = "transfers.internal";
    public const string TransfersReturn = "transfers.return";

    // Production
    public const string ProductionStages = "production.stages";
    public const string ProductionRecipes = "production.recipes";
    public const string ProductionOrders = "production.orders";

    // Quality
    public const string QcParameters = "qc.parameters";
    public const string QcChecks = "qc.checks";

    // Finance
    public const string FinanceTransactions = "finance.transactions";
    public const string FinanceDebts = "finance.debts";
    public const string FinancePayments = "finance.payments";

    // Counterparties
    public const string CounterpartiesSuppliers = "counterparties.suppliers";
    public const string CounterpartiesClients = "counterparties.clients";
    public const string CounterpartiesPortal = "counterparties.portal";

    // KPI
    public const string KpiShifts = "kpi.shifts";
    public const string KpiPlans = "kpi.plans";
    public const string KpiAttendance = "kpi.attendance";
    public const string KpiEfficiency = "kpi.efficiency";

    // Delivery
    public const string DeliveryFleet = "delivery.fleet";
    public const string DeliveryRoutes = "delivery.routes";

    // Agents
    public const string AgentsCommissions = "agents.commissions";

    // AI (F10)
    /// <summary>AI yordamchisi (web paneli va Telegram).</summary>
    /// <remarks>
    /// ⚠️ Bu feature SUKUT BO'YICHA O'CHIQ va hech bir planga KIRMAYDI: har chaqiriq
    /// pul turadi, shuning uchun uni Console operatori tenantga OSHKORA yoqadi.
    /// Boshqa feature'lar (qaysi biri plandan kelishi) bepul yuzalar — ular uchun
    /// «sukut bo'yicha yoqiq» to'g'ri, bu yerda esa hisobni ko'paytirardi.
    /// </remarks>
    public const string AiChat = "ai.chat";

    // Cross-cutting
    public const string AnalyticsAdvanced = "analytics.advanced";
    public const string ExportExcel = "export.excel";
    public const string ExportPdf = "export.pdf";
    public const string ImportExcel = "import.excel";

    /// Prefix reserved for one-customer features (S5). Custom codes never appear in a plan.
    public const string CustomPrefix = "custom.";
}
