using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Tenancy;

/// <summary>
/// Yangi tenantning bazaviy mazmuni: TIZIM rollari (ruxsatlari bilan) va o'lchov birliklari.
/// IDEMPOTENT — yetishmaganini qo'shadi, mavjudiga tegmaydi.
/// </summary>
/// <remarks>
/// <para>
/// SQLite davridagi <c>TenantProvisioner</c> ning o'rnini bosadi, lekin tenant va admin
/// foydalanuvchini YARATMAYDI: reyestr Identity'da (P4), tenant nusxasini va profilni JIT
/// yozadi (<c>WmsPlatformUserSink</c>). Bu yerda faqat tenant ichidagi boshlang'ich holat.
/// </para>
/// <para>
/// Tizim rollari mavjud bo'lsa ularning ruxsatlariga tegilmaydi — tenant admini ularni nozik
/// sozlagan bo'lishi mumkin (D5).
/// </para>
/// </remarks>
public sealed class TenantBaseline
{
    /// <summary>Har zavodda bir xil birliklar (eski <c>TenantProvisioner.DefaultUnits</c>).</summary>
    private static readonly (string Name, string ShortName)[] DefaultUnits =
    [
        ("Kilogramm", "kg"),
        ("Litr", "litr"),
        ("Dona", "dona"),
        ("Gramm", "gramm"),
        ("Quti", "quti"),
        ("Paket", "paket"),
    ];

    private readonly WmsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly IWmsAccessResolver _access;

    public TenantBaseline(WmsDbContext db, ICurrentTenant currentTenant, IWmsAccessResolver access)
    {
        _db = db;
        _currentTenant = currentTenant;
        _access = access;
    }

    /// <remarks>
    /// Tenant kontekstini O'ZI qo'yadi: chaqiruvchi (JIT, demo seed, fon vazifa) uni qo'ymagan
    /// bo'lishi mumkin, RLS esa kontekstsiz na o'qishga, na yozishga ruxsat beradi.
    /// </remarks>
    public async Task EnsureAsync(Guid tenantId, string? tenantCode, CancellationToken cancellationToken = default)
    {
        _currentTenant.Set(tenantId, tenantCode);

        HashSet<string> existing = new(
            await _db.Roles.Where(r => r.Code != null).Select(r => r.Code!).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        foreach (string code in WmsSystemRoles.All.Where(c => !existing.Contains(c)))
        {
            Role role = new()
            {
                Code = code,
                IsSystem = true,
                Name = WmsSystemRoles.DisplayName(code),
            };

            foreach (string permission in WmsSystemRoles.PermissionsFor(code))
            {
                role.RolePermissions.Add(new RolePermission { PermissionCode = permission });
            }

            _db.Roles.Add(role);
        }

        if (!await _db.Units.AnyAsync(cancellationToken))
        {
            _db.Units.AddRange(DefaultUnits.Select(u => new Unit { Name = u.Name, ShortName = u.ShortName }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Rol yangidan paydo bo'ldi — «rol yo'q» deb keshlangan javoblar bekor bo'lsin.
        _access.InvalidateTenant(tenantId);
    }
}
