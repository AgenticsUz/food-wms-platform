namespace WMS.Application.Interfaces;

/// <summary>
/// Joriy so'rov egasi — JWT claim'lari + WMS'ning RBAC bazasi.
/// </summary>
/// <remarks>
/// SQLite davridagi <c>BaseController.UserId</c> (<c>int</c>, o'z JWT'dan) o'rnini bosadi.
/// Ikki identifikator bor va ular ARALASHMASIN:
/// <list type="bullet">
///   <item><see cref="Sub"/> — Identity <c>sub</c> (global odam);</item>
///   <item><see cref="ProfileId"/> — shu tenantdagi <c>user_profile.id</c>; WMS jadvallaridagi
///   har <c>*UserId</c> ustuni SHUNGA ishora qiladi.</item>
/// </list>
/// </remarks>
public interface ICurrentUser
{
    /// <summary>Identity <c>sub</c>.</summary>
    Guid? Sub { get; }

    /// <summary>Joriy tenantdagi <c>user_profile.id</c>; profil yo'q bo'lsa (Console operatori) <see langword="null"/>.</summary>
    Guid? ProfileId { get; }

    string? FullName { get; }
    bool IsAuthenticated { get; }

    /// <summary><c>is_platform_admin</c> — faqat ishonchli emitent tokenidan (fail-closed).</summary>
    bool IsPlatformAdmin { get; }

    /// <summary>Platforma admini yoki Console'ning <c>wms.admin</c> roli (§4.4).</summary>
    bool IsConsoleOperator { get; }

    /// <summary>Amaldagi ruxsatlar — FAQAT RBAC bazasidan (tokenda ruxsat yo'q).</summary>
    IReadOnlySet<string> Permissions { get; }

    bool HasPermission(string permission);
}

/// <summary>Foydalanuvchining tenant ichidagi amaldagi huquqlari (so'rov boshida bir marta yechiladi).</summary>
/// <param name="ProfileId"><c>user_profile.id</c>.</param>
/// <param name="FullName">Ism.</param>
/// <param name="Permissions">Ruxsat kodlari.</param>
public sealed record WmsAccess(Guid ProfileId, string FullName, IReadOnlySet<string> Permissions);

/// <summary>So'rov davomidagi <see cref="WmsAccess"/> (scoped).</summary>
public sealed class WmsAccessContext
{
    public WmsAccess? Access { get; private set; }
    public bool IsResolved { get; private set; }

    public void Set(WmsAccess? access)
    {
        Access = access;
        IsResolved = true;
    }
}

/// <summary>
/// Huquqlar manbai: <c>user_profile(identity_sub) → user_role → role_permission</c>, qisqa kesh bilan.
/// </summary>
/// <remarks>
/// ⚠️ Rol yoki ruxsatni O'ZGARTIRADIGAN har kod (rollar ekrani, foydalanuvchi rollari)
/// keshni bekor qilishi SHART — aks holda o'zgarish kesh muddati o'tguncha kuchga
/// kirmaydi va administrator ham, foydalanuvchi ham sababini topa olmaydi.
/// </remarks>
public interface IWmsAccessResolver
{
    /// <summary>Joriy tenant kontekstida yechadi; profil yo'q yoki faol emas — <see langword="null"/>.</summary>
    ValueTask<WmsAccess?> ResolveAsync(Guid sub, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Bitta foydalanuvchining keshini bekor qiladi.</summary>
    void Invalidate(Guid sub, Guid tenantId);

    /// <summary>Tenantdagi hamma foydalanuvchining keshini bekor qiladi (rol ruxsatlari o'zgarganda).</summary>
    void InvalidateTenant(Guid tenantId);
}
