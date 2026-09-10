using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// <c>wms.user_profile</c> — Identity foydalanuvchisining shu tenantdagi NUSXASI.
/// </summary>
/// <remarks>
/// <para>
/// SQLite davridagi <c>User</c> jadvalining o'rnini bosadi (D5). Parol, lockout,
/// <c>IsSuperAdmin</c>, <c>SecurityStamp</c>, <c>MustChangePassword</c> —
/// hammasi Identity'da (§3.1) va bu yerda YO'Q. WMS foydalanuvchi YARATMAYDI:
/// profil birinchi tokenda JIT yoziladi (<c>WmsPlatformUserSink</c>), odamni
/// tenantga Console biriktiradi (D7).
/// </para>
/// <para>
/// ⚠️ WMS jadvallaridagi har <c>*UserId</c> (<c>CreatedByUserId</c>,
/// <c>RecordedByUserId</c>, <c>WorkerUserId</c> ...) — <b>shu jadvalning</b>
/// <see cref="BaseEntity.Id"/> si, Identity <c>sub</c> EMAS. <c>sub</c> faqat
/// <see cref="IdentitySub"/> da turadi. Console operatori (profili yo'q) bajargan
/// amallar esa <c>*BySub</c> ustunlariga yoziladi.
/// </para>
/// </remarks>
public class UserProfile : TenantEntity
{
    /// <summary>Identity <c>sub</c>. <c>(tenant_id, identity_sub)</c> noyob.</summary>
    public Guid IdentitySub { get; set; }

    public string FullName { get; set; } = null!;

    /// <summary>
    /// Telefon (E.164). ⚠️ Tokenda <c>phone</c> scope'isiz kelmaydi — bo'sh qiymat
    /// saqlangan raqamni O'CHIRMAYDI (sink izohi).
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>Telegram bildirishnomalari uchun (foydalanuvchi o'zi ulaydi).</summary>
    public string? TelegramChatId { get; set; }

    /// <summary>WMS ichidagi o'chirgich — Identity'dagi bloklashdan mustaqil.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
