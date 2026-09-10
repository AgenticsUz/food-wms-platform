using Microsoft.AspNetCore.Mvc;
using WMS.API.Surfaces;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Platform;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Admin;

/// <summary>
/// WMS control plane'ining tenantga bog'lanMAGAN qismi (Console WMS bo'limi): ko'rsatkichlar, muddati
/// tugayotganlar, planlar va feature katalogi. Hammasi platforma jadvallari — tenant konteksti kerak emas.
/// </summary>
/// <remarks>
/// <para>
/// F6: SQLite davridagi <c>/api/admin/*</c> (SuperAdmin) O'CHDI — o'rniga Console yuzasi (§4.4,
/// <c>AdminBaseController</c> siyosati). Marshrut shakllari Wash'ning <c>/admin/v1</c> iga mos: Console'ning
/// WMS bo'limi Wash bo'limining qo'shnisi. Tenant kartasi amallari <see cref="TenantsController"/> da.
/// </para>
/// <para>
/// O'CHDI (qaytarilmasin): tenant yaratish/o'chirish (Identity'da, P4), modul yoqish (D6), lidlar (D9),
/// tashkilotlar (D10), parol tiklash (D5 — Identity admin API'si), platforma auditi (§4.4 dagi
/// <c>GET /admin/v1/audit</c> — <c>ControlPlaneController</c>).
/// </para>
/// </remarks>
[Route("admin/v1")]
public sealed class AdminController : AdminBaseController
{
    private readonly ITenantService _tenants;
    private readonly IPlanService _plans;
    private readonly IBillingService _billing;
    private readonly IFeatureService _features;

    public AdminController(ITenantService tenants, IPlanService plans, IBillingService billing, IFeatureService features)
    {
        _tenants = tenants;
        _plans = plans;
        _billing = billing;
        _features = features;
    }

    // ── Ko'rsatkichlar ───────────────────────────────────────────────────

    [HttpGet("stats")]
    public async Task<ActionResult<AdminResponse<PlatformStatsDto>>> GetStats(CancellationToken ct)
        => Ok(AdminResponse<PlatformStatsDto>.Ok(await _tenants.GetStatsAsync(ct)));

    /// Subscriptions running out within N days — the platform dashboard's "call these people" list.
    [HttpGet("tenants/expiring")]
    public async Task<ActionResult<AdminResponse<List<ExpiringTenantDto>>>> GetExpiring([FromQuery] int days = 7, CancellationToken ct = default)
        => Ok(AdminResponse<List<ExpiringTenantDto>>.Ok(await _billing.GetExpiringAsync(days, ct)));

    // ── Planlar ──────────────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<ActionResult<AdminResponse<List<PlanDto>>>> GetPlans(CancellationToken ct)
        => Ok(AdminResponse<List<PlanDto>>.Ok(await _plans.GetPlansAsync(ct)));

    [HttpPost("plans")]
    public async Task<ActionResult<AdminResponse<PlanDto>>> CreatePlan([FromBody] CreatePlanDto dto, CancellationToken ct)
    {
        PlanDto plan = await _plans.CreatePlanAsync(dto, ct);
        return Created($"{WmsSurfaces.AdminPrefix}/plans/{plan.Id}", AdminResponse<PlanDto>.Ok(plan));
    }

    [HttpPut("plans/{id:guid}")]
    public async Task<ActionResult<AdminResponse<PlanDto>>> UpdatePlan(Guid id, [FromBody] CreatePlanDto dto, CancellationToken ct)
        => Ok(AdminResponse<PlanDto>.Ok(await _plans.UpdatePlanAsync(id, dto, ct)));

    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        await _plans.DeletePlanAsync(id, ct);
        return NoContent();
    }

    // ── Feature katalogi (S4, S5) ────────────────────────────────────────

    [HttpGet("features")]
    public async Task<ActionResult<AdminResponse<List<FeatureDto>>>> GetFeatures([FromQuery] bool? isCustom = null, CancellationToken ct = default)
        => Ok(AdminResponse<List<FeatureDto>>.Ok(await _features.GetCatalogAsync(isCustom, ct)));

    /// <summary>
    /// Yangi feature — asosan bitta mijoz uchun yozilgan <c>custom.*</c> (qoidalar
    /// <c>WMS.Application/Features/Custom/README.md</c> da, reyestr <c>docs/CUSTOM_FEATURES.md</c>).
    /// </summary>
    [HttpPost("features")]
    public async Task<ActionResult<AdminResponse<FeatureDto>>> CreateFeature([FromBody] CreateFeatureDto dto, CancellationToken ct)
    {
        FeatureDto feature = await _features.CreateAsync(dto, ct);
        return Created($"{WmsSurfaces.AdminPrefix}/features", AdminResponse<FeatureDto>.Ok(feature));
    }
}
