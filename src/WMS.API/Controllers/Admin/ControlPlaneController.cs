using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.API.Surfaces;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.API.Controllers.Admin;

/// <summary>
/// <c>/admin/v1</c> ning MAJBURIY to'rtligi (PLATFORMA-TZ §4.4): yuza tirikligi, mahsulot registri,
/// ruxsat katalogi, control plane jurnali. Shakllar Console'ning <c>product-admin.model.ts</c> iga
/// qarab yozilgan — HRM va Wash bilan bir xil, Console har mahsulotga alohida sozlama saqlamasin.
/// </summary>
/// <remarks>
/// WMS'ning O'Z control plane'i (tenant kartasi, planlar, to'lovlar, feature, brendlash) boshqa
/// controller'larda: ular WMS biznesidan, bu to'rttasi esa §4.4 tahriridan o'zgaradi.
/// </remarks>
[Route("admin/v1")]
public sealed class ControlPlaneController : AdminBaseController
{
    private const string ProductName = "Agentics WMS";

    /// <summary>
    /// Katalog kodda (<see cref="WmsPermissions"/>), ya'ni faqat deploy bilan o'zgaradi — halol
    /// «ro'yxatdan o'tgan» vaqt shu jarayon ko'tarilgan lahza (har so'rovda <c>UtcNow</c> Console'da
    /// «hozirgina yangilandi» degan yolg'on berardi).
    /// </summary>
    private static readonly DateTimeOffset CatalogRegisteredAt = DateTimeOffset.UtcNow;

    /// <summary>Modul nomlari (SQLite davridagi <c>Module</c> seed'idan).</summary>
    private static readonly Dictionary<string, string> ModuleNames = new(StringComparer.Ordinal)
    {
        [ModuleCodes.WarehouseRaw] = "Raw Material Warehouse",
        [ModuleCodes.Production] = "Production",
        [ModuleCodes.WarehouseFinished] = "Finished Goods Warehouse",
        [ModuleCodes.Transfers] = "Transfer System",
        [ModuleCodes.Finance] = "Finance",
        [ModuleCodes.Kpi] = "KPI & Shifts",
        [ModuleCodes.Suppliers] = "Suppliers",
        [ModuleCodes.Clients] = "Clients",
        [ModuleCodes.Quality] = "Quality Control",
        [ModuleCodes.Agents] = "Agents",
        [ModuleCodes.Delivery] = "Delivery",
    };

    private readonly WmsDbContext _db;

    public ControlPlaneController(WmsDbContext db) => _db = db;

    /// <summary>Yuza tirikligi (ilova salomatligi <c>/health</c> da). Siyosat bilan yopiq — versiya oshkor bo'lmasin.</summary>
    [HttpGet("health")]
    public ActionResult<AdminResponse<WmsSurfaceHealth>> Health() =>
        Ok(AdminResponse<WmsSurfaceHealth>.Ok(new WmsSurfaceHealth(WmsSurfaces.ProductCode, WmsSurfaces.AdminVersion, WmsSurfaces.HealthyStatus, DateTimeOffset.UtcNow)));

    /// <summary>WMS'ning O'Z registri — bitta mahsulot va uning modullari (Identity katalogidagi yozuv emas).</summary>
    [HttpGet("products")]
    public ActionResult<AdminResponse<IReadOnlyList<ProductAdminProductDto>>> Products()
    {
        IReadOnlyList<ProductAdminModuleDto> modules =
        [
            .. ModuleCodes.All.Select(code => new ProductAdminModuleDto(
                StableId($"{WmsSurfaces.ProductCode}:module:{code}"), code, ModuleNames.GetValueOrDefault(code, code), true)),
        ];

        IReadOnlyList<ProductAdminProductDto> products =
            [new(StableId($"{WmsSurfaces.ProductCode}:product"), WmsSurfaces.ProductCode, ProductName, modules, WmsPermissions.All.Count)];

        return Ok(AdminResponse<IReadOnlyList<ProductAdminProductDto>>.Ok(products));
    }

    /// <summary>
    /// Nozik ruxsat katalogi. Begona <c>?product=</c> — bo'sh ro'yxat, 404 emas: Console bir xil
    /// chaqiruvni har mahsulotga yuboradi va 404 uning ekranini yiqitardi.
    /// </summary>
    [HttpGet("permission-catalog")]
    public ActionResult<AdminResponse<IReadOnlyList<ProductAdminPermissionDto>>> PermissionCatalog([FromQuery] string? product)
    {
        IReadOnlyList<ProductAdminPermissionDto> items =
            product is { Length: > 0 } && !string.Equals(product, WmsSurfaces.ProductCode, StringComparison.OrdinalIgnoreCase)
                ? []
                : [.. WmsPermissions.All
                    .OrderBy(p => p.Code, StringComparer.Ordinal)
                    .Select(p => new ProductAdminPermissionDto(p.Code, p.Name, p.Group, WmsSurfaces.ProductCode, CatalogRegisteredAt))];

        return Ok(AdminResponse<IReadOnlyList<ProductAdminPermissionDto>>.Ok(items));
    }

    /// <summary>
    /// Control plane jurnali — Console bajargan amallar (<c>is_platform_action</c>). Tenant OSHKORA:
    /// <c>filter[tenantId]</c> yoki <c>X-Tenant-Id</c>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Tenantsiz jurnal MUMKIN EMAS: <c>audit_log</c> RLS ostida va ilova roli NOBYPASSRLS —
    /// kontekstsiz so'rov 0 qator berardi va «hech narsa bo'lmagan» degan YOLG'ON ko'rinish chiqardi.
    /// Shuning uchun tenant ko'rsatilmasa 403 (Wash bilan bir xil).
    /// </remarks>
    [HttpGet("audit")]
    public async Task<ActionResult<AdminResponse<IReadOnlyList<ProductAdminAuditEntryDto>>>> Audit(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery(Name = "filter[tenantId]")] string? tenantFilter = null,
        [FromQuery(Name = "filter[action]")] string? actionFilter = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        Guid? tenantId = Guid.TryParse(tenantFilter, out Guid parsed)
            ? parsed
            : HttpContext.RequestServices.GetRequiredService<ICurrentTenant>().TenantId;

        if (tenantId is not { } selected)
        {
            throw new ForbiddenException("Tenant is required: pass filter[tenantId] or the X-Tenant-Id header");
        }

        UseTenant(selected);
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);

        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking().Where(a => a.IsPlatformAction);

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            string action = actionFilter.Trim().ToLowerInvariant();
            query = query.Where(a => (a.EntityType + "." + (a.EntityAction ?? string.Empty)).ToLower() == action);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Path, pattern) || (a.UserName != null && EF.Functions.ILike(a.UserName, pattern)));
        }

        long total = await query.LongCountAsync(cancellationToken);
        List<AuditLog> rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ProductAdminAuditEntryDto> items = [.. rows.Select(ToConsoleEntry)];
        return Ok(AdminResponse<IReadOnlyList<ProductAdminAuditEntryDto>>.Paged(items, page, size, total));
    }

    /// <summary>
    /// <c>action</c> — <c>&lt;controller&gt;.&lt;amal&gt;</c> (HRM/Wash konvensiyasi); <c>payload</c> — JSON
    /// MATN, ichida yo'l va yozuv id'si (Console modelida ular uchun maydon yo'q, tashlansa yo'qolardi).
    /// </summary>
    private static ProductAdminAuditEntryDto ToConsoleEntry(AuditLog entry) => new(
        entry.Id,
        new DateTimeOffset(DateTime.SpecifyKind(entry.CreatedAt, DateTimeKind.Utc)),
        $"{entry.EntityType}.{entry.EntityAction}".ToLowerInvariant(),
        entry.TenantId,
        entry.UserName,
        JsonSerializer.Serialize(new { method = entry.Action, path = entry.Path, entityId = entry.EntityId, status = entry.StatusCode, actorSub = entry.ActorSub }),
        entry.CorrelationId);

    /// <summary>
    /// Kod satridan BARQAROR id: modullar baza qatori emas, Console esa <c>id</c> kutadi; tasodifiy
    /// Guid har startupda o'zgarib, Console ro'yxatidagi tanlov «yo'qolardi».
    /// </summary>
    private static Guid StableId(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
}

public sealed record ProductAdminModuleDto(Guid Id, string Code, string Name, bool EnabledByDefault);

public sealed record ProductAdminProductDto(Guid Id, string Code, string Name, IReadOnlyList<ProductAdminModuleDto> Modules, int PermissionCount);

public sealed record ProductAdminPermissionDto(string Code, string Name, string GroupCode, string ProductCode, DateTimeOffset RegisteredAt);

public sealed record ProductAdminAuditEntryDto(Guid Id, DateTimeOffset At, string Action, Guid? TenantId, string? ActorLogin, string Payload, string? CorrelationId);

internal static class AdminFormat
{
    public static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);
}
