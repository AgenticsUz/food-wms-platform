using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Audit;
using WMS.Application.DTOs.Common;
using WMS.Application.DTOs.Plans;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Branding;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Users;
using WMS.Application.DTOs.Tenants;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

/// <summary>
/// Platform control plane. All endpoints are SuperAdmin-only and operate cross-tenant.
/// </summary>
[Route("api/admin")]
[Authorize(Policy = "SuperAdmin")]
public class AdminController : BaseController
{
    private readonly ITenantService _tenants;
    private readonly IPlanService _plans;
    private readonly IBillingService _billing;
    private readonly ILeadService _leads;
    private readonly IFeatureService _features;
    private readonly IOrganizationService _organizations;
    private readonly IBrandingService _branding;
    private readonly IPasswordResetService _passwords;
    private readonly IAuditService _audit;

    public AdminController(ITenantService tenants, IPlanService plans, IBillingService billing,
        ILeadService leads, IFeatureService features, IOrganizationService organizations,
        IBrandingService branding, IPasswordResetService passwords, IAuditService audit)
    {
        _passwords = passwords;
        _audit = audit;
        _tenants = tenants;
        _plans = plans;
        _billing = billing;
        _leads = leads;
        _features = features;
        _organizations = organizations;
        _branding = branding;
    }

    // ── Tenants ──────────────────────────────────────────────────────────

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
        => Ok(ApiResponse<List<TenantDto>>.Ok(await _tenants.GetAllAsync()));

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.CreateAsync(dto)));

    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(int id, [FromBody] UpdateTenantDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.UpdateAsync(id, dto)));

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(int id)
    { await _tenants.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    /// Body is optional: an empty request still suspends (reason "Other"), which keeps the
    /// old one-click behaviour working while the console catches up.
    [HttpPut("tenants/{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(int id, [FromBody] SuspendTenantDto? dto = null)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.SuspendAsync(id, dto, UserId)));

    [HttpPut("tenants/{id}/activate")]
    public async Task<IActionResult> ActivateTenant(int id)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.ActivateAsync(id)));

    [HttpPut("tenants/{id}/plan")]
    public async Task<IActionResult> AssignPlan(int id, [FromBody] AssignPlanDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _tenants.AssignPlanAsync(id, dto.PlanId)));

    [HttpGet("tenants/{id}/modules")]
    public async Task<IActionResult> GetTenantModules(int id)
        => Ok(ApiResponse<List<TenantModuleDto>>.Ok(await _tenants.GetModulesAsync(id)));

    [HttpPut("tenants/{id}/modules")]
    public async Task<IActionResult> ToggleTenantModules(int id, [FromBody] ToggleModulesRequest request)
    {
        if (request.Modules is { Count: > 0 })
            await _tenants.ToggleModulesAsync(id, request.Modules);
        else if (request.ModuleId > 0)
            await _tenants.ToggleModuleAsync(id, new ToggleModuleDto { ModuleId = request.ModuleId, IsEnabled = request.IsEnabled });
        return Ok(ApiResponse<object>.Ok(null!, "Updated"));
    }

    // ── Plans ────────────────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
        => Ok(ApiResponse<List<PlanDto>>.Ok(await _plans.GetPlansAsync()));

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto)
        => Ok(ApiResponse<PlanDto>.Ok(await _plans.CreatePlanAsync(dto)));

    [HttpPut("plans/{id}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] CreatePlanDto dto)
        => Ok(ApiResponse<PlanDto>.Ok(await _plans.UpdatePlanAsync(id, dto)));

    [HttpDelete("plans/{id}")]
    public async Task<IActionResult> DeletePlan(int id)
    { await _plans.DeletePlanAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // ── Manual billing (S1) ──────────────────────────────────────────────

    [HttpPost("tenants/{id}/payments")]
    public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentDto dto)
        => Ok(ApiResponse<PaymentRecordDto>.Ok(await _billing.RecordPaymentAsync(id, dto, UserId),
            "Payment recorded"));

    [HttpGet("tenants/{id}/payments")]
    public async Task<IActionResult> GetPayments(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<PaymentRecordDto>>.Ok(await _billing.GetPaymentsAsync(id, page, pageSize)));

    [HttpDelete("payments/{paymentId}")]
    public async Task<IActionResult> DeletePayment(int paymentId)
    {
        var tenantId = await _billing.DeletePaymentAsync(paymentId);
        // The audit row belongs to the tenant whose payment was cancelled, not to the
        // platform tenant — the route only carries a payment id, so we pass it explicitly.
        HttpContext.Items["AuditTargetTenantId"] = tenantId;
        return Ok(ApiResponse<object>.Ok(null!, "Payment cancelled"));
    }

    /// Subscriptions running out within N days — the platform dashboard's "call these people" list.
    [HttpGet("tenants/expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 7)
        => Ok(ApiResponse<List<ExpiringTenantDto>>.Ok(await _billing.GetExpiringAsync(days)));

    // ── Leads (S3) ───────────────────────────────────────────────────────

    [HttpGet("leads")]
    public async Task<IActionResult> GetLeads([FromQuery] LeadStatus? status, [FromQuery] LeadSource? source,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<LeadDto>>.Ok(await _leads.GetAllAsync(status, source, search, page, pageSize)));

    [HttpGet("leads/{id}")]
    public async Task<IActionResult> GetLead(int id)
        => Ok(ApiResponse<LeadDto>.Ok(await _leads.GetByIdAsync(id)));

    [HttpPut("leads/{id}")]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] UpdateLeadDto dto)
        => Ok(ApiResponse<LeadDto>.Ok(await _leads.UpdateAsync(id, dto)));

    [HttpPost("leads/{id}/convert")]
    public async Task<IActionResult> ConvertLead(int id, [FromBody] ConvertLeadDto dto)
        => Ok(ApiResponse<TenantDto>.Ok(await _leads.ConvertAsync(id, dto), "Tenant created"));

    // ── Features (S4, S5) ────────────────────────────────────────────────

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures([FromQuery] bool? isCustom = null)
        => Ok(ApiResponse<List<FeatureDto>>.Ok(await _features.GetCatalogAsync(isCustom)));

    [HttpPost("features")]
    public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureDto dto)
        => Ok(ApiResponse<FeatureDto>.Ok(await _features.CreateAsync(dto)));

    [HttpGet("tenants/{id}/features")]
    public async Task<IActionResult> GetTenantFeatures(int id)
        => Ok(ApiResponse<List<TenantFeatureDto>>.Ok(await _features.GetTenantFeaturesAsync(id)));

    [HttpPut("tenants/{id}/features")]
    public async Task<IActionResult> SetTenantFeatures(int id, [FromBody] SetTenantFeaturesDto dto)
    {
        await _features.SetTenantFeaturesAsync(id, dto, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Updated"));
    }

    // ── Users and password recovery ───────────────────────────────────────
    // There is no self-service "forgot my password" yet, so when a customer locks itself
    // out the only way back in is a phone call and one of these two endpoints.

    [HttpGet("tenants/{id}/users")]
    public async Task<IActionResult> GetTenantUsers(int id, CancellationToken ct)
        => Ok(ApiResponse<List<TenantUserDto>>.Ok(await _passwords.GetTenantUsersAsync(id, ct)));

    /// <summary>
    /// Sets a new password for one user of a tenant. Both body fields are optional: with no
    /// userId the tenant's admin is resolved, with no password one is generated.
    ///
    /// The password is in the RESPONSE ONLY — it is never stored in plain text, never
    /// written to the audit log, and no other endpoint will ever show it again.
    /// </summary>
    [HttpPost("tenants/{id}/reset-user-password")]
    public async Task<IActionResult> ResetUserPassword(int id, [FromBody] ResetUserPasswordDto? dto,
        CancellationToken ct)
        => Ok(ApiResponse<PasswordResetResultDto>.Ok(
            await _passwords.ResetByPlatformAsync(id, dto ?? new ResetUserPasswordDto(), UserId, ct),
            "Password reset"));

    // ── Branding (B1) ────────────────────────────────────────────────────
    // Only SuperAdmin uploads a logo. Letting a tenant admin rebrand its own installation
    // is a separate decision with its own moderation problem.

    [HttpPost("tenants/{id}/logo")]
    [RequestSizeLimit(1_048_576)]   // 1 MB at the pipe; the service enforces the real 512 KB
    public async Task<IActionResult> UploadLogo(int id, [FromQuery] string type, IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail(Messages.LogoEmpty));

        var kind = ParseLogoKind(type);
        await using var stream = file.OpenReadStream();
        var upload = new LogoUpload(stream, file.FileName, file.ContentType, file.Length);

        var branding = await _branding.UploadLogoAsync(id, kind, upload, ct);
        return Ok(ApiResponse<BrandingDto>.Ok(branding, "Updated"));
    }

    [HttpDelete("tenants/{id}/logo")]
    public async Task<IActionResult> DeleteLogo(int id, [FromQuery] string type, CancellationToken ct)
        => Ok(ApiResponse<BrandingDto>.Ok(
            await _branding.RemoveLogoAsync(id, ParseLogoKind(type), ct), "Deleted"));

    [HttpGet("tenants/{id}/branding")]
    public async Task<IActionResult> GetBranding(int id, CancellationToken ct)
        => Ok(ApiResponse<BrandingDto>.Ok(await _branding.GetAsync(id, ct)));

    private static LogoKind ParseLogoKind(string? type) => (type ?? "").ToLowerInvariant() switch
    {
        "square" => LogoKind.Square,
        _ => LogoKind.Wide
    };

    // ── Organizations (S6) ───────────────────────────────────────────────

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations([FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(ApiResponse<List<OrganizationDto>>.Ok(await _organizations.SearchAsync(search, page, pageSize)));

    [HttpGet("organizations/{id}")]
    public async Task<IActionResult> GetOrganization(int id)
        => Ok(ApiResponse<OrganizationDetailDto>.Ok(await _organizations.GetByIdAsync(id)));

    // ── Modules & stats ──────────────────────────────────────────────────

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules()
        => Ok(ApiResponse<List<ModuleInfoDto>>.Ok(await _tenants.GetModuleCatalogAsync()));

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(ApiResponse<PlatformStatsDto>.Ok(await _tenants.GetStatsAsync()));

    // ── Audit (platforma ko'rinishi) ─────────────────────────────────────
    // `/api/audit` JWT'dagi TenantId bilan chegaralangan — SuperAdmin u orqali boshqa
    // tenantning izini ko'ra olmaydi. Shu sababli alohida endpoint.

    /// <summary>
    /// Barcha tenantlar bo'yicha audit. `tenantId` berilmasa — hammasi.
    /// Sahifalash majburiy (default 50, maksimal 200): audit eng tez o'sadigan jadval.
    /// </summary>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] AdminAuditQuery query, CancellationToken ct)
        => Ok(ApiResponse<PaginatedList<AuditLogDto>>.Ok(await _audit.GetPlatformLogsAsync(query, ct)));

    /// Filtr uchun amal turlari — barcha tenantlar bo'yicha.
    /// Tenant ro'yxati uchun alohida endpoint yozilmadi: mavjud `GET /api/admin/tenants`
    /// allaqachon id + nom qaytaradi va admin konsoli uni baribir yuklab turadi.
    [HttpGet("audit/entity-types")]
    public async Task<IActionResult> GetAuditEntityTypes(CancellationToken ct)
        => Ok(ApiResponse<List<string>>.Ok(await _audit.GetPlatformEntityTypesAsync(ct)));
}
