using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Platform.Infrastructure.Identity;
using Platform.SharedKernel.Querying;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Access;

/// <summary>
/// Amaldagi huquqlarni <c>user_profile(identity_sub) → user_role → role_permission</c> dan yechadi,
/// 5 daqiqalik xotira keshi bilan (bitta replika, Redis yo'q — Wash naqshi).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davrida <c>[RequirePermission]</c> HAR so'rovda bazaga JOIN yuborardi. Endi
/// huquqlar so'rov boshida bir marta yechiladi (<c>EffectiveAccessMiddleware</c>) va
/// atributlar xotiradagi to'plamni tekshiradi.
/// </para>
/// <para>
/// Paketning <see cref="IEffectiveAccessResolver"/> ini ham amalga oshiradi va
/// <c>IsAuthoritative = true</c> qaytaradi: shunda <c>PlatformUserSyncMiddleware</c>
/// ruxsatlarni RoleMap xaritasidan YOZMAYDI — xarita WMS'da faqat JIT'dagi boshlang'ich
/// rol uchun, tenant admini nozik sozlagan ruxsatni har so'rovda bosib ketmasin.
/// </para>
/// <para>
/// Tenant bo'yicha bekor qilish — versiya hisoblagichi orqali (<see cref="IMemoryCache"/>
/// prefiks bo'yicha o'chira olmaydi).
/// </para>
/// </remarks>
public sealed class WmsAccessResolver : IWmsAccessResolver, IEffectiveAccessResolver
{
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly WmsDbContext _db;
    private readonly IMemoryCache _cache;

    public WmsAccessResolver(WmsDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public bool IsAuthoritative => true;

    public async ValueTask<WmsAccess?> ResolveAsync(Guid sub, Guid tenantId, CancellationToken cancellationToken = default)
    {
        string key = UserKey(sub, tenantId, TenantVersion(tenantId));
        if (_cache.TryGetValue(key, out WmsAccess? cached))
        {
            return cached;
        }

        WmsAccess? loaded = await LoadAsync(sub, cancellationToken);
        _cache.Set(key, loaded, CacheTtl);
        return loaded;
    }

    public void Invalidate(Guid sub, Guid tenantId) => _cache.Remove(UserKey(sub, tenantId, TenantVersion(tenantId)));

    public void InvalidateTenant(Guid tenantId) =>
        _cache.Set(VersionKey(tenantId), TenantVersion(tenantId) + 1, TimeSpan.FromDays(1));

    async ValueTask<EffectiveAccess> IEffectiveAccessResolver.ResolveAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        WmsAccess? access = await ResolveAsync(userId, tenantId, cancellationToken);
        return access is null
            ? EffectiveAccess.None
            : new EffectiveAccess(access.Permissions, DataScope.All, []);
    }

    ValueTask IEffectiveAccessResolver.InvalidateAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        Invalidate(userId, tenantId);
        return ValueTask.CompletedTask;
    }

    private async Task<WmsAccess?> LoadAsync(Guid sub, CancellationToken cancellationToken)
    {
        // Tenant filtri + RLS avtomatik: chaqiruvchi tenant kontekstini qo'ygan.
        var profile = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.IdentitySub == sub && u.IsActive)
            .Select(u => new { u.Id, u.FullName })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        string[] codes = await _db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == profile.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp.PermissionCode)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        // Katalogda yo'q kod (eski yozuv, qo'lda kiritilgan) hech narsa ochmaydi.
        HashSet<string> permissions = new(codes.Where(WmsPermissions.IsKnown), StringComparer.Ordinal);

        return new WmsAccess(profile.Id, profile.FullName, permissions);
    }

    private long TenantVersion(Guid tenantId) => _cache.TryGetValue(VersionKey(tenantId), out long version) ? version : 0;

    private static string VersionKey(Guid tenantId) => string.Create(CultureInfo.InvariantCulture, $"access-ver:{tenantId:N}");

    private static string UserKey(Guid sub, Guid tenantId, long version) =>
        string.Create(CultureInfo.InvariantCulture, $"access:{tenantId:N}:{version}:{sub:N}");
}
