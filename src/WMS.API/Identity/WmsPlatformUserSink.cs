using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using Platform.Web.UserSync;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.API.Identity;

/// <summary>
/// Tokendagi tenant va foydalanuvchini WMS bazasiga JIT yozadi (PLATFORMA-TZ §3.7, §5.4; Wash naqshi).
/// </summary>
/// <remarks>
/// <para>
/// F6 dan keyin WMS na tenant, na foydalanuvchi YARATADI (D5, D7): Console'da zavodga
/// biriktirilgan odam birinchi marta kelganda jadvalda na tenant, na profil, na rol bo'ladi va
/// usiz har so'rov 403 bo'lardi. To'rt qadam, har biri idempotent:
/// </para>
/// <list type="number">
///   <item><c>wms.tenant</c> nusxasi (+ yangi tenantga sukut plani va trial);</item>
///   <item>tenant YANGI bo'lsa — <see cref="TenantBaseline"/> (tizim rollari, birliklar);</item>
///   <item><c>wms.user_profile</c> — <c>identity_sub</c> bo'yicha ism va telefon;</item>
///   <item>yirik rol ↔ tizim roli sinxronizatsiyasi.</item>
/// </list>
/// <para>
/// ⚠️ 4-qadam FAQAT O'ZI bergan tizim rolini almashtiradi (<c>user_profile.identity_role</c>
/// esda tutadi): Identity'da <c>admin → manager</c> qilingani WMS'da ham ko'rinsin, lekin
/// tenant admini qo'shgan MAXSUS rollarni token har 15 daqiqada bosib ketmasin.
/// ⚠️ Tenant konteksti QO'LDA va TOKENDAGI tenantga qo'yiladi (<c>X-Tenant-Id</c> ga emas):
/// sink <c>TenantResolutionMiddleware</c> dan oldin turadi.
/// </para>
/// </remarks>
internal sealed partial class WmsPlatformUserSink : IPlatformUserSink
{
    private readonly WmsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly TenantBaseline _baseline;
    private readonly IWmsAccessResolver _access;
    private readonly ITenantStateService _tenantState;
    private readonly SubscriptionOptions _options;
    private readonly ILogger<WmsPlatformUserSink> _logger;
    private readonly IOpsNotifier _ops;

    public WmsPlatformUserSink(
        WmsDbContext db,
        ICurrentTenant currentTenant,
        TenantBaseline baseline,
        IWmsAccessResolver access,
        ITenantStateService tenantState,
        IOptions<SubscriptionOptions> options,
        IOpsNotifier ops,
        ILogger<WmsPlatformUserSink> logger)
    {
        _ops = ops;
        _db = db;
        _currentTenant = currentTenant;
        _baseline = baseline;
        _access = access;
        _tenantState = tenantState;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Identity'dagi tenant kodi shakli (<c>AdminInput.TenantCodePattern</c>).</summary>
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantCodePattern();

    public async ValueTask SyncAsync(PlatformUserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Console tokenida `tenant_id` yo'q (§4.4) — operator zavod xodimi emas, profil yozilmaydi.
        // Bu xato emas: aks holda har admin so'rovi `user_sync_failed` berardi.
        if (profile.TenantId is not { } tenantId || tenantId == Guid.Empty)
        {
            return;
        }

        _currentTenant.Set(tenantId, profile.TenantCode);

        if (await SyncTenantAsync(tenantId, profile, cancellationToken) is not { } created)
        {
            return;
        }

        if (created)
        {
            await _baseline.EnsureAsync(tenantId, profile.TenantCode, cancellationToken);
            _currentTenant.Set(tenantId, profile.TenantCode);
        }

        UserProfile stored = await SyncProfileAsync(profile, cancellationToken);
        await SyncRoleAsync(tenantId, profile, stored, cancellationToken);
    }

    /// <returns><see langword="true"/> — shu chaqiruvda yaratildi; <see langword="null"/> — yozib bo'lmadi (fail-closed).</returns>
    private async Task<bool?> SyncTenantAsync(Guid tenantId, PlatformUserProfile profile, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        Tenant? tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            if (string.IsNullOrWhiteSpace(profile.TenantCode) || !TenantCodePattern().IsMatch(profile.TenantCode))
            {
                _logger.LogError(
                    "Tenant {TenantId} tokenidagi kod shaklga mos emas ('{TenantCode}') — nusxa yozilmadi (fail-closed)",
                    tenantId, profile.TenantCode);

                // Nusxasi yo'q tenant ostida davom etish keyingi so'rovlarga 0 qator o'rniga
                // tushunarsiz FK xatolarini berardi.
                _currentTenant.Clear();
                return null;
            }

            tenant = Tenant.FromToken(tenantId, profile.TenantCode, profile.Modules, now);

            // Tijorat qatlami WMS'niki (D6): yangi zavod sukut (trial) planida boshlaydi. Plan
            // bo'lmasa (baza seed qilinmagan) trial baribir muddatli — aks holda obuna cheksiz
            // bepul bo'lib qolardi (eski TenantProvisioner'dagi dars).
            Plan? plan = await _db.Plans.AsNoTracking()
                .Where(p => p.IsActive && p.IsDefault)
                .FirstOrDefaultAsync(cancellationToken);

            tenant.PlanId = plan?.Id;
            tenant.SubscriptionStatus = SubscriptionStatus.Trial;
            tenant.TrialEndsAt = now.AddDays(plan is { TrialDays: > 0 } ? plan.TrialDays : _options.TrialDays);

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(cancellationToken);

            // Platforma egasiga (TG17): yangi zavod birinchi marta kirdi — Console'ga qaramasdan bilsin.
            await _ops.SendAsync(
                $"🆕 Yangi tenant: <b>{System.Net.WebUtility.HtmlEncode(tenant.Code)}</b> — trial {tenant.TrialEndsAt:dd.MM.yyyy} gacha",
                $"ops:tenant-new:{tenant.Id:N}", cancellationToken);
            return true;
        }

        // Nom tokenda yo'q (`name` — ODAMNING ismi): nusxaning nomini Console qo'yadi.
        if (tenant.SyncFromToken(profile.TenantCode, profile.Modules, now))
        {
            await _db.SaveChangesAsync(cancellationToken);

            // Modul to'plami o'zgargan bo'lishi mumkin — `[RequireModule]` kesh oynasini kutmasin.
            _tenantState.Invalidate(tenantId);
        }

        return false;
    }

    private async Task<UserProfile> SyncProfileAsync(PlatformUserProfile profile, CancellationToken cancellationToken)
    {
        UserProfile? stored = await _db.UserProfiles.FirstOrDefaultAsync(u => u.IdentitySub == profile.UserId, cancellationToken);

        string fullName = string.IsNullOrWhiteSpace(profile.FullName)
            ? profile.PhoneNumber ?? profile.UserId.ToString("N")
            : profile.FullName.Trim();

        if (stored is null)
        {
            stored = new UserProfile
            {
                IdentitySub = profile.UserId,
                FullName = fullName,
                Phone = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? null : profile.PhoneNumber,
                LastSeenAt = DateTime.UtcNow,
            };

            _db.UserProfiles.Add(stored);
            await _db.SaveChangesAsync(cancellationToken);
            return stored;
        }

        // ⚠️ Bo'sh telefon «telefon yo'q» degani EMAS: `phone_number` scope'isiz tokenda kelmaydi.
        // Uni o'zgarish deb olsak saqlangan raqam o'chib, Telegram/SMS xabarlari jim yetmay qolardi.
        string? phone = string.IsNullOrWhiteSpace(profile.PhoneNumber) ? stored.Phone : profile.PhoneNumber;

        if (!string.Equals(stored.FullName, fullName, StringComparison.Ordinal)
            || !string.Equals(stored.Phone, phone, StringComparison.Ordinal))
        {
            stored.FullName = fullName;
            stored.Phone = phone;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return stored;
    }

    /// <summary>
    /// Tokendagi yirik rolni WMS tizim roliga o'giradi va farq bo'lsa ALMASHTIRADI.
    /// </summary>
    /// <remarks>
    /// Uch holat: (1) <c>identity_role</c> bo'sh va rol yo'q — birinchi kirish, biriktiriladi;
    /// (2) <c>identity_role</c> bo'sh, lekin profilda AYNAN bitta tizim roli bor — eski
    /// ma'lumot, o'sha rol «JIT bergan» deb qabul qilinadi; (3) farq bor — eski tizim roli
    /// olib tashlanadi, yangisi qo'shiladi, maxsus rollar joyida qoladi.
    /// </remarks>
    private async Task SyncRoleAsync(Guid tenantId, PlatformUserProfile profile, UserProfile stored, CancellationToken cancellationToken)
    {
        string? systemRole = WmsSystemRoles.FromTokenRoles(profile.Roles);
        if (systemRole is null)
        {
            _logger.LogWarning(
                "Foydalanuvchi {UserId} tokenidagi yirik rollar WMS xaritasida yo'q — rol o'zgartirilmadi (fail-closed)",
                profile.UserId);
            return;
        }

        List<UserRole> current = await _db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == stored.Id)
            .ToListAsync(cancellationToken);

        // Eski profil (ustun F8 dan oldin yozilgan): bitta tizim roli bo'lsa, uni JIT
        // bergan deb qabul qilamiz — aks holda birinchi sinxronizatsiya ikkinchi tizim
        // rolini qo'shib yuborardi.
        string? previous = stored.IdentityRole;
        if (previous is null)
        {
            List<UserRole> systemRoles = [.. current.Where(ur => IsSystemRole(ur.Role))];
            previous = systemRoles.Count == 1 ? systemRoles[0].Role!.Code : null;
        }

        if (string.Equals(previous, systemRole, StringComparison.Ordinal)
            && current.Any(ur => string.Equals(ur.Role?.Code, systemRole, StringComparison.Ordinal)))
        {
            // Ustun hali to'ldirilmagan bo'lsa — shu yerda yoziladi (keyingi solishtiruv arzon).
            if (stored.IdentityRole is null)
            {
                stored.IdentityRole = systemRole;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        Guid? roleId = await FindRoleAsync(systemRole, cancellationToken);
        if (roleId is null)
        {
            // Nusxa bor, tizim rollari yo'q (masalan qo'lda yozilgan tenant) — baseline idempotent.
            await _baseline.EnsureAsync(tenantId, profile.TenantCode, cancellationToken);
            _currentTenant.Set(tenantId, profile.TenantCode);
            roleId = await FindRoleAsync(systemRole, cancellationToken);
        }

        if (roleId is not { } granted)
        {
            _logger.LogWarning("Tenant {TenantId} da '{RoleCode}' tizim roli yo'q — rol o'zgartirilmadi", tenantId, systemRole);
            return;
        }

        // Faqat AVVAL JIT bergan tizim roli olib tashlanadi: admin qo'shgan boshqa rollar
        // (tizim roli bo'lsa ham — bu uning ongli qarori) joyida qoladi.
        if (previous is not null)
        {
            List<UserRole> stale = [.. current.Where(ur => string.Equals(ur.Role?.Code, previous, StringComparison.Ordinal))];
            if (stale.Count > 0)
            {
                _db.UserRoles.RemoveRange(stale);
            }
        }

        if (!current.Any(ur => ur.RoleId == granted))
        {
            _db.UserRoles.Add(new UserRole { UserId = stored.Id, RoleId = granted });
        }

        stored.IdentityRole = systemRole;
        await _db.SaveChangesAsync(cancellationToken);

        // Kesh eski ruxsatlarni allaqachon yozgan bo'lishi mumkin — usiz rolni pasaytirish
        // 5 daqiqa kuchga kirmasdi (yoki yangi odam 5 daqiqa 403 ko'rardi).
        _access.Invalidate(profile.UserId, tenantId);

        _logger.LogInformation(
            "Foydalanuvchi {UserId} tizim roli '{Previous}' → '{RoleCode}' (Identity roli o'zgardi)",
            profile.UserId, previous ?? "—", systemRole);
    }

    private static bool IsSystemRole(Role? role) =>
        role?.Code is { } code && WmsSystemRoles.Ordered.Contains(code, StringComparer.Ordinal);

    private Task<Guid?> FindRoleAsync(string code, CancellationToken cancellationToken) =>
        _db.Roles.Where(r => r.Code == code).Select(r => (Guid?)r.Id).FirstOrDefaultAsync(cancellationToken);
}
