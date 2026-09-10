using Microsoft.AspNetCore.Mvc;
using WMS.API.Surfaces;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Branding;
using WMS.Application.DTOs.Plans;
using WMS.Application.DTOs.Platform;
using WMS.Application.DTOs.Tenants;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Admin;

/// <summary>
/// Console'ning WMS bo'limi — tenant ro'yxati va kartasi: plan, trial, suspend, feature override,
/// to'lovlar, brendlash, foydalanuvchilar (faqat o'qish).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <c>/tenants/{id}/...</c> dagi HAR amal birinchi so'rovdan OLDIN <see cref="AdminBaseController.UseTenant"/>
/// ni chaqiradi: <c>tenant_feature</c>, <c>payment_record</c>, <c>user_profile</c>, <c>audit_log</c> RLS ostida
/// va Console tokenida <c>tenant_id</c> yo'q — kontekstsiz so'rov jimgina 0 qator berardi. Servislar buni
/// tekshiradi va kontekst bo'lmasa yiqiladi. Tenant tanlovi auditga ham shu kontekst orqali tushadi
/// (mijoz o'z jurnalida kim uni to'xtatganini ko'radi).
/// </para>
/// <para>
/// Har yozuvdan keyin <c>ITenantStateService.Invalidate</c> servislarda — o'zgarish wms-web'ga keshni
/// kutmay yetadi. SQLite davridagi <c>/api/tenants</c> (SuperAdmin + tenant modul ko'rinishi) O'CHDI:
/// modullar Identity'da (D6), tenant o'z modullarini <c>/api/me</c> dan ko'radi.
/// </para>
/// </remarks>
[Route("admin/v1/tenants")]
public sealed class TenantsController : AdminBaseController
{
    private readonly ITenantService _tenants;
    private readonly IFeatureService _features;
    private readonly IBillingService _billing;
    private readonly IBrandingService _branding;
    private readonly IUserService _users;

    public TenantsController(ITenantService tenants, IFeatureService features, IBillingService billing,
        IBrandingService branding, IUserService users)
    {
        _tenants = tenants;
        _features = features;
        _billing = billing;
        _branding = branding;
        _users = users;
    }

    /// Console operatorining Identity <c>sub</c>'i — uning WMS profili yo'q, amallar <c>*BySub</c> ga yoziladi.
    private Guid? OperatorSub => HttpContext.RequestServices.GetRequiredService<ICurrentUser>().Sub;

    // ── Ro'yxat va karta ─────────────────────────────────────────────────

    /// <summary>Sahifalangan ro'yxat: <c>?page=&amp;size=&amp;search=&amp;filter[status]=trial|active|suspended|inactive</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<AdminResponse<List<TenantListItemDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null,
        [FromQuery(Name = "filter[status]")] string? status = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);
        var (items, total) = await _tenants.ListAsync(page, size, search, status, ct);
        return Ok(AdminResponse<List<TenantListItemDto>>.Paged(items, page, size, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.GetAsync(id, ct)));
    }

    /// <summary>Ko'rsatiladigan nom (HOLAT §4 #9), trial/to'lov sanasi, WMS o'chirgichi. <c>null</c> — o'zgarmaydi.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> Update(Guid id, [FromBody] UpdateTenantDto dto, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.UpdateAsync(id, dto, ct)));
    }

    /// <summary><c>{ planId }</c>; <c>null</c> — planni olib tashlash.</summary>
    [HttpPut("{id:guid}/plan")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> AssignPlan(Guid id, [FromBody] AssignPlanDto dto, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.AssignPlanAsync(id, dto.PlanId, ct)));
    }

    /// Body is optional: an empty request still suspends (reason "other").
    [HttpPost("{id:guid}/suspend")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> Suspend(Guid id, [FromBody] SuspendTenantDto? dto, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.SuspendAsync(id, dto, OperatorSub, ct)));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> Activate(Guid id, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.ActivateAsync(id, ct)));
    }

    // ── Feature override'lari ────────────────────────────────────────────

    [HttpGet("{id:guid}/features")]
    public async Task<ActionResult<AdminResponse<List<TenantFeatureDto>>>> GetFeatures(Guid id, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<List<TenantFeatureDto>>.Ok(await _features.GetTenantFeaturesAsync(id, ct)));
    }

    /// <summary>Tana — ro'yxatning O'ZI (Wash bilan bir xil); <c>enabled: null</c> override'ni o'chiradi.</summary>
    [HttpPut("{id:guid}/features")]
    public async Task<ActionResult<AdminResponse<TenantDetailDto>>> SetFeatures(Guid id, [FromBody] List<FeatureOverrideInput> overrides, CancellationToken ct)
    {
        UseTenant(id);
        await _features.SetTenantFeaturesAsync(id, overrides ?? [], OperatorSub, ct);
        return Ok(AdminResponse<TenantDetailDto>.Ok(await _tenants.GetAsync(id, ct)));
    }

    // ── Qo'lda billing (S1) ──────────────────────────────────────────────

    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult<AdminResponse<List<PaymentRecordDto>>>> GetPayments(Guid id,
        [FromQuery] int page = 1, [FromQuery] int size = 50, CancellationToken ct = default)
    {
        UseTenant(id);
        return Ok(AdminResponse<List<PaymentRecordDto>>.Ok(await _billing.GetPaymentsAsync(id, page, size, ct)));
    }

    /// <summary>
    /// <c>paid_until</c> faqat OLDINGA suriladi; trial tugaydi; <c>nonPayment</c> suspend olib tashlanadi —
    /// Console chaqiruvdan keyin kartani qayta yuklasin.
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<AdminResponse<PaymentRecordDto>>> RecordPayment(Guid id, [FromBody] RecordPaymentDto dto, CancellationToken ct)
    {
        UseTenant(id);
        PaymentRecordDto payment = await _billing.RecordPaymentAsync(id, dto, OperatorSub, ct);
        return Created($"{WmsSurfaces.AdminPrefix}/tenants/{id}/payments/{payment.Id}", AdminResponse<PaymentRecordDto>.Ok(payment));
    }

    /// <summary>
    /// ⚠️ Wash'dagi <c>DELETE /payments/{paymentId}</c> dan farqli — tenant ostida: <c>payment_record</c>
    /// RLS ostida va tenantsiz kontekstda to'lov topilmasdi. O'chirilgach <c>paid_until</c> qolgan
    /// to'lovlardan qayta hisoblanadi.
    /// </summary>
    [HttpDelete("{id:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, Guid paymentId, CancellationToken ct)
    {
        UseTenant(id);
        await _billing.DeletePaymentAsync(id, paymentId, ct);
        return NoContent();
    }

    // ── Brendlash (D14) ──────────────────────────────────────────────────

    [HttpGet("{id:guid}/branding")]
    public async Task<ActionResult<AdminResponse<BrandingDto>>> GetBranding(Guid id, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<BrandingDto>.Ok(await _branding.GetAsync(id, ct)));
    }

    /// <summary>Faqat rang (<c>{ brandColor }</c>); logo alohida — <c>multipart</c>.</summary>
    [HttpPut("{id:guid}/branding")]
    public async Task<ActionResult<AdminResponse<BrandingDto>>> UpdateBranding(Guid id, [FromBody] UpdateBrandingDto dto, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<BrandingDto>.Ok(await _branding.SetColorAsync(id, dto.BrandColor, ct)));
    }

    /// <summary><c>multipart/form-data</c>, maydon <c>file</c>; <c>?type=wide|square</c> (sukut — wide).</summary>
    [HttpPost("{id:guid}/logo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1_048_576)]   // 1 MB at the pipe; the service enforces the real 512 KB
    public async Task<ActionResult<AdminResponse<BrandingDto>>> UploadLogo(Guid id, [FromQuery] string? type, IFormFile? file, CancellationToken ct)
    {
        UseTenant(id);
        if (file == null || file.Length == 0)
            throw new AppException(Messages.LogoEmpty);

        await using var stream = file.OpenReadStream();
        var upload = new LogoUpload(stream, file.FileName, file.ContentType, file.Length);
        return Ok(AdminResponse<BrandingDto>.Ok(await _branding.UploadLogoAsync(id, ParseLogoKind(type), upload, ct)));
    }

    [HttpDelete("{id:guid}/logo")]
    public async Task<ActionResult<AdminResponse<BrandingDto>>> DeleteLogo(Guid id, [FromQuery] string? type, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<BrandingDto>.Ok(await _branding.RemoveLogoAsync(id, ParseLogoKind(type), ct)));
    }

    private static LogoKind ParseLogoKind(string? type) => (type ?? "").ToLowerInvariant() switch
    {
        "square" => LogoKind.Square,
        _ => LogoKind.Wide
    };

    // ── Foydalanuvchilar (faqat o'qish, support uchun) ───────────────────

    /// <summary>
    /// Zavoddagi WMS profillari va rollari. Yaratish/bloklash/parol — Console'ning Identity bo'limida
    /// (D5, D7); nozik rollarni tenant admini wms-web'da sozlaydi.
    /// </summary>
    [HttpGet("{id:guid}/users")]
    public async Task<ActionResult<AdminResponse<List<UserDto>>>> GetUsers(Guid id, CancellationToken ct)
    {
        UseTenant(id);
        return Ok(AdminResponse<List<UserDto>>.Ok(await _users.GetAllAsync(ct)));
    }
}
