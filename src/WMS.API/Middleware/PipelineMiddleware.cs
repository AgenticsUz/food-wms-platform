using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Tenancy;
using WMS.API.Authorization;
using WMS.API.Surfaces;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Middleware;

/// <summary>
/// Tenant konteksti: BIRLAMCHI — JWT <c>tenant_id</c>. <c>X-Tenant-Id</c> faqat ikki holatda o'qiladi:
/// admin yuzasidagi Console operatori va tenant yuzasidagi platforma admini. Boshqalar uchun
/// sarlavha e'tiborsiz (fail-closed) — aks holda zavod administratori o'z tokeni bilan qo'shni
/// zavodni so'rab ololardi.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        ClaimsPrincipal user = context.User;
        Guid? tenantId = null;
        string? tenantCode = null;

        if (user.Identity?.IsAuthenticated == true
            && Guid.TryParse(user.FindFirstValue(PlatformClaimNames.TenantId), out Guid fromClaim))
        {
            tenantId = fromClaim;
            tenantCode = user.FindFirstValue(PlatformClaimNames.TenantCode);
        }

        bool mayOverride = WmsSurfaces.IsAdminPath(context.Request.Path)
            ? WmsAdminSurface.IsConsoleOperator(user)
            : currentUser.IsPlatformAdmin;

        if (mayOverride
            && context.Request.Headers.TryGetValue(TenantHeaderName, out StringValues header)
            && Guid.TryParse(header.ToString(), out Guid fromHeader))
        {
            _logger.LogInformation("tenant_selected: {Sub} X-Tenant-Id orqali {TenantId} kontekstida", currentUser.Sub, fromHeader);
            tenantId = fromHeader;
            tenantCode = null;
        }

        if (tenantId is { } value && value != Guid.Empty)
        {
            currentTenant.Set(value, tenantCode);
        }

        await _next(context);
    }
}

/// <summary>
/// So'rov boshida amaldagi huquqlarni BIR MARTA yechadi (<c>user_profile → user_role → role_permission</c>).
/// </summary>
/// <remarks>
/// SQLite davrida <c>[RequirePermission]</c> har atributda bazaga JOIN yuborardi. Manba javob
/// bermasa — huquq yo'q (fail-closed), log bilan.
/// </remarks>
public sealed class EffectiveAccessMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EffectiveAccessMiddleware> _logger;

    public EffectiveAccessMiddleware(RequestDelegate next, ILogger<EffectiveAccessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, ICurrentTenant currentTenant, IWmsAccessResolver resolver, WmsAccessContext accessContext)
    {
        if (!accessContext.IsResolved && currentUser.IsAuthenticated
            && currentUser.Sub is { } sub && currentTenant.TenantId is { } tenantId)
        {
            try
            {
                accessContext.Set(await resolver.ResolveAsync(sub, tenantId, context.RequestAborted));
            }
#pragma warning disable CA1031 // Manba qanday uzilishidan qat'i nazar natija bir xil: huquq berilmaydi.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                _logger.LogError(exception, "Foydalanuvchi {Sub} (tenant {TenantId}) huquqlarini yechib bo'lmadi — ruxsatsiz davom etadi", sub, tenantId);
                accessContext.Set(null);
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Obunani HAR autentifikatsiyalangan so'rovda tekshiradi: to'xtatish token muddatini kutmasin.
/// Bloklansa 402 va mashina o'qiydigan <c>code</c> (<see cref="SubscriptionPolicy"/>).
/// </summary>
/// <remarks>
/// Ozod: <c>/api/me</c> va <c>/api/subscription</c> (bloklangan mijoz NEGA ekanini ko'rsin),
/// <c>/admin/v1</c> (Console to'xtatilgan zavodga to'lovni kiritib, obunani TIKLAY olsin),
/// infra yo'llari va platforma admini.
/// </remarks>
public sealed class SubscriptionEnforcementMiddleware
{
    private static readonly string[] ExemptPrefixes =
        ["/api/me", "/api/subscription", WmsSurfaces.AdminPrefix, "/health", "/alive", "/scalar", "/openapi", "/uploads"];

    private readonly RequestDelegate _next;
    private readonly SubscriptionOptions _options;

    public SubscriptionEnforcementMiddleware(RequestDelegate next, IOptions<SubscriptionOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, ICurrentTenant currentTenant, ITenantStateService tenantState)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        if (!currentUser.IsAuthenticated || currentUser.IsPlatformAdmin || currentTenant.TenantId is not { } tenantId
            || Array.Exists(ExemptPrefixes, p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        TenantState? state = await tenantState.GetAsync(tenantId, context.RequestAborted);
        SubscriptionVerdict verdict = SubscriptionPolicy.Evaluate(state, _options, DateTime.UtcNow);
        if (verdict.Allowed)
        {
            await _next(context);
            return;
        }

        string message = verdict.PublicMessage ?? Translations.Format(verdict.Message!, RequestLanguage.Resolve(context));
        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message, verdict.Code!), WmsJson.Options);
    }
}

/// <summary>Middleware'lar qo'lda yozadigan javoblarning JSON sozlamasi (MVC bilan bir xil camelCase).</summary>
public static class WmsJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
