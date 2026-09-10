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
///   <item>profilda HALI rol yo'q bo'lsa — yirik roldan tizim roli.</item>
/// </list>
/// <para>
/// ⚠️ 4-qadam FAQAT bo'sh holatda: aks holda tenant admini bergan rolni Identity tokeni har
/// 15 daqiqada bosib ketardi. ⚠️ Tenant konteksti QO'LDA va TOKENDAGI tenantga qo'yiladi
/// (<c>X-Tenant-Id</c> ga emas): sink <c>TenantResolutionMiddleware</c> dan oldin turadi.
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

    public WmsPlatformUserSink(
        WmsDbContext db,
        ICurrentTenant currentTenant,
        TenantBaseline baseline,
        IWmsAccessResolver access,
        ITenantStateService tenantState,
        IOptions<SubscriptionOptions> options,
        ILogger<WmsPlatformUserSink> logger)
    {
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
        await SeedRoleAsync(tenantId, profile, stored, cancellationToken);
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

    private async Task SeedRoleAsync(Guid tenantId, PlatformUserProfile profile, UserProfile stored, CancellationToken cancellationToken)
    {
        if (await _db.UserRoles.AnyAsync(ur => ur.UserId == stored.Id, cancellationToken))
        {
            return;
        }

        string? systemRole = WmsSystemRoles.FromTokenRoles(profile.Roles);
        if (systemRole is null)
        {
            _logger.LogWarning(
                "Foydalanuvchi {UserId} tokenidagi yirik rollar WMS xaritasida yo'q — rol biriktirilmadi (fail-closed)",
                profile.UserId);
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
            _logger.LogWarning("Tenant {TenantId} da '{RoleCode}' tizim roli yo'q — foydalanuvchi rolsiz qoldi", tenantId, systemRole);
            return;
        }

        _db.UserRoles.Add(new UserRole { UserId = stored.Id, RoleId = granted });
        await _db.SaveChangesAsync(cancellationToken);

        // Kesh «rol yo'q» javobini allaqachon yozgan bo'lishi mumkin — usiz yangi odam 5 daqiqa 403 ko'rardi.
        _access.Invalidate(profile.UserId, tenantId);

        _logger.LogInformation("Foydalanuvchi {UserId} birinchi kirishida '{RoleCode}' tizim roliga biriktirildi (JIT)", profile.UserId, systemRole);
    }

    private Task<Guid?> FindRoleAsync(string code, CancellationToken cancellationToken) =>
        _db.Roles.Where(r => r.Code == code).Select(r => (Guid?)r.Id).FirstOrDefaultAsync(cancellationToken);
}
