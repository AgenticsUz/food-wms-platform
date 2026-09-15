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

    // Telegram ulanishi — <see cref="TelegramLink"/> jadvalida (ilgari shu yerda `TelegramChatId` edi).

    /// <summary>
    /// Tokendagi YIRIK rol (<c>admin</c>/<c>manager</c>/<c>employee</c>/<c>viewer</c>),
    /// oxirgi marta JIT shu profilga qaysi tizim rolini bergan bo'lsa — o'sha.
    /// </summary>
    /// <remarks>
    /// ⚠️ Nega alohida ustun kerak: JIT tokendagi rolni WMS rollari bilan taqqoslay
    /// olmaydi — tenant admini tizim rolini boshqa rol bilan almashtirgan bo'lishi
    /// mumkin. Bu ustun «JIT nima berganini» eslab turadi, shunda Identity'da rol
    /// o'zgarganda AYNAN o'sha rol almashtiriladi va admin qo'shgan maxsus rollarga
    /// tegilmaydi. <see langword="null"/> — hali sinxronlanmagan (eski profil).
    /// </remarks>
    public string? IdentityRole { get; set; }

    /// <summary>WMS ichidagi o'chirgich — Identity'dagi bloklashdan mustaqil.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Shu xodim uchun standart ombor — tenant sozlamasini BOSIB ketadi.</summary>
    /// <remarks>
    /// Sex xodimi doim bitta omborda ishlaydi; tenant sozlamasi esa umumiy. Bo'sh bo'lsa
    /// tenant sozlamasi ishlatiladi, u ham bo'sh bo'lsa — forma omborni so'raydi.
    /// </remarks>
    public Guid? DefaultWarehouseId { get; set; }

    /// <summary>Standart ombor (navigatsiya).</summary>
    public Warehouse? DefaultWarehouse { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
