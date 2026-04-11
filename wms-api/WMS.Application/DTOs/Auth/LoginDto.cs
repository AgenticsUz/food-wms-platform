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
    public List<string> Roles { get; set; } = new();
    public List<string> EnabledModules { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
