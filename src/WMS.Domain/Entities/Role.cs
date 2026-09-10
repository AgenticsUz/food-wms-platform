using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// Tenant ichidagi rol — nozik ruxsatlar to'plami (Q4: «yirik rol Identity'da,
/// nozik ruxsat mahsulotda»).
/// </summary>
/// <remarks>
/// <see cref="Code"/> to'ldirilgan rol — TIZIM roli (<c>admin</c>, <c>manager</c>,
/// <c>employee</c>, <c>viewer</c>): u yangi tenantda avtomatik yaratiladi va JIT
/// odamni Identity'dagi yirik roli bo'yicha SHUNGA biriktiradi. Tenant admini
/// tizim rolining ruxsatlarini ham, o'z rollarini ham nozik sozlaydi.
/// </remarks>
public class Role : TenantEntity
{
    /// <summary>Tizim roli kodi yoki <see langword="null"/> (tenant yaratgan rol).</summary>
    public string? Code { get; set; }

    public bool IsSystem { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Rolning bitta ruxsati. Katalog KODDA (<c>WmsPermissions</c>), bazada emas —
/// SQLite davridagi <c>Permission</c> jadvali o'chdi: katalog faqat deploy bilan
/// o'zgaradi va jadval ikkinchi manba bo'lib ajralib ketardi.
/// </summary>
public class RolePermission : TenantEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string PermissionCode { get; set; } = null!;
}

/// <summary>Profil ↔ rol.</summary>
public class UserRole : TenantEntity
{
    /// <summary><c>user_profile.id</c> (Identity <c>sub</c> EMAS).</summary>
    public Guid UserId { get; set; }
    public UserProfile User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
