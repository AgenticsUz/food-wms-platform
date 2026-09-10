namespace WMS.Application.Common;

/// Module codes seeded in <c>WmsDbContext</c>. Used by <c>RequireModuleAttribute</c> and by
/// plan definitions — keep in sync with the Module seed data (ids 1–11).
public static class ModuleCodes
{
    public const string WarehouseRaw = "WAREHOUSE_RAW";
    public const string Production = "PRODUCTION";
    public const string WarehouseFinished = "WAREHOUSE_FINISHED";
    public const string Transfers = "TRANSFERS";
    public const string Finance = "FINANCE";
    public const string Kpi = "KPI";
    public const string Suppliers = "SUPPLIERS";
    public const string Clients = "CLIENTS";
    public const string Quality = "QUALITY";
    public const string Agents = "AGENTS";
    public const string Delivery = "DELIVERY";

    public static readonly string[] All =
    [
        WarehouseRaw, Production, WarehouseFinished, Transfers, Finance,
        Kpi, Suppliers, Clients, Quality, Agents, Delivery
    ];
}
