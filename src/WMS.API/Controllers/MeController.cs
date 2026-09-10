using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Identity;
using Platform.Infrastructure.Tenancy;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

/// <summary>
/// <c>GET /api/me</c> — wms-web'ning <c>CurrentUserStore</c> manbai: kim, qaysi zavod, qaysi
/// ruxsat/modul/feature, brendlash va obuna holati BITTA javobda.
/// </summary>
/// <remarks>
/// <para>
/// SQLite davrida bu ma'lumot login javobida kelardi va 7 kun tokenda qotib qolardi: rol yoki
/// modul o'zgarsa foydalanuvchi qayta kirmaguncha eski menyuni ko'rardi. Endi har yuklanishda
/// shu yerdan (keshlangan holatdan) olinadi.
/// </para>
/// <para>
/// <c>RequireTenant</c> YO'Q va obuna tekshiruvidan OZOD: to'xtatilgan zavod ham NEGA
/// to'xtatilganini ko'rsin, tenantsiz platforma admini esa bo'sh tenant bilan javob olsin.
/// Brendlash ham shu yerda (D14) — login sahifasi Identity'da va u neytral.
/// </para>
/// </remarks>
[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ICurrentUser _user;
    private readonly ICurrentTenant _tenant;
    private readonly ITenantStateService _tenantState;
    private readonly SubscriptionOptions _options;

    public MeController(ICurrentUser user, ICurrentTenant tenant, ITenantStateService tenantState, IOptions<SubscriptionOptions> options)
    {
        _user = user;
        _tenant = tenant;
        _tenantState = tenantState;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<MeDto>>> Get(CancellationToken cancellationToken)
    {
        TenantState? state = _tenant.TenantId is { } tenantId ? await _tenantState.GetAsync(tenantId, cancellationToken) : null;
        DateTime now = DateTime.UtcNow;

        MeTenantDto? tenant = state is null
            ? null
            : new MeTenantDto(state.Id, state.Code, state.Name, state.LogoUrl, state.LogoSquareUrl, state.BrandColor);

        MeSubscriptionDto? subscription = null;
        if (state is not null)
        {
            SubscriptionVerdict verdict = SubscriptionPolicy.Evaluate(state, _options, now);
            subscription = new MeSubscriptionDto(
                state.Status.ToString(),
                verdict.Allowed,
                verdict.Code,
                verdict.Message is null ? null : Translations.Format(verdict.Message, RequestLanguage.Resolve(HttpContext)),
                verdict.PublicMessage,
                state.PlanCode,
                state.PlanName,
                state.TrialEndsAt,
                state.PaidUntil,
                SubscriptionPolicy.TrialDaysLeft(state, now),
                SubscriptionPolicy.PaidDaysLeft(state.PaidUntil, now));
        }

        MeDto me = new(
            _user.Sub ?? Guid.Empty,
            _user.ProfileId,
            _user.FullName ?? string.Empty,
            User.FindFirstValue(PlatformClaimNames.PhoneNumber),
            _user.IsPlatformAdmin,
            tenant,
            [.. ReadAll(User, PlatformClaimNames.Roles)],
            [.. _user.Permissions.Order(StringComparer.Ordinal)],
            [.. (state?.EnabledModules ?? []).Order(StringComparer.Ordinal)],
            [.. (state?.EnabledFeatures ?? []).Order(StringComparer.Ordinal)],
            subscription);

        return Ok(ApiResponse<MeDto>.Ok(me));
    }

    /// <summary>Identity ko'p qiymatli claim'ni massiv ham, bo'sh joyli satr ham qilib yuborishi mumkin (F1 stendi).</summary>
    private static IEnumerable<string> ReadAll(ClaimsPrincipal principal, string type) =>
        principal.FindAll(type)
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);
}

public sealed record MeDto(
    Guid Sub,
    Guid? ProfileId,
    string FullName,
    string? Phone,
    bool IsPlatformAdmin,
    MeTenantDto? Tenant,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Features,
    MeSubscriptionDto? Subscription);

public sealed record MeTenantDto(Guid Id, string Code, string Name, string? LogoUrl, string? LogoSquareUrl, string? BrandColor);

public sealed record MeSubscriptionDto(
    string Status,
    bool Allowed,
    string? Code,
    string? Message,
    string? PublicMessage,
    string? PlanCode,
    string? PlanName,
    DateTime? TrialEndsAt,
    DateTime? PaidUntil,
    int? TrialDaysLeft,
    int? PaidDaysLeft);
