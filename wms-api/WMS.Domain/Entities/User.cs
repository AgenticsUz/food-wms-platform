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
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
