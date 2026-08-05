using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class User : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    // Platform egasi — barcha tenantlarni boshqara oladi (control plane). Oddiy tenant admini emas.
    public bool IsSuperAdmin { get; set; } = false;
    // Telegram bildirishnomalari uchun (foydalanuvchi o'zi ulaydi; ixtiyoriy)
    public string? TelegramChatId { get; set; }

    /// <summary>
    /// Tokenlarni bekor qilish uchun. Parol o'zgarganda yangilanadi; JWT ichidagi nusxa
    /// mos kelmasa token rad etiladi. NULL — paroli hech qachon tiklanmagan foydalanuvchi
    /// (eski tokenlari ishlashda davom etadi, ya'ni deploy hech kimni chiqarib yubormaydi).
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// Oxirgi muvaffaqiyatli kirish — SuperAdmin "kim hali kira oladi" ro'yxati uchun.
    public DateTime? LastLoginAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
