namespace WMS.Application.DTOs.Auth;

public class LoginDto
{
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string TenantSlug { get; set; } = null!;
}

public class AuthResponseDto
{
    public string Token { get; set; } = null!;
    public UserInfoDto User { get; set; } = null!;
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public int TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public bool IsSuperAdmin { get; set; }
    public string? TelegramChatId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> EnabledModules { get; set; } = new();
    /// Finer-grained entitlements resolved for this tenant (feature layer).
    public List<string> EnabledFeatures { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class SetTelegramDto
{
    public string? ChatId { get; set; }
}

public class RegisterDto
{
    public string TenantName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}
