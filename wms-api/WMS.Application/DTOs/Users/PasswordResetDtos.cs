namespace WMS.Application.DTOs.Users;

/// <summary>
/// SuperAdmin resetting a password inside a tenant. Both fields are optional:
/// no userId means "the tenant's admin" (the case that actually happens — a one-person
/// factory locked out of its own system), and no password means "make one up".
/// </summary>
public class ResetUserPasswordDto
{
    public int? UserId { get; set; }
    public string? NewPassword { get; set; }
}

/// A tenant admin resetting one of their own staff.
public class ResetPasswordDto
{
    public string? NewPassword { get; set; }
}

/// <summary>
/// The only place the new password is ever returned. It is not stored anywhere in plain
/// text, not written to the audit log, and no other endpoint will show it again — if the
/// operator loses it, they reset again.
/// </summary>
public class PasswordResetResultDto
{
    public int UserId { get; set; }
    /// The phone the user signs in with.
    public string Login { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// One row of the SuperAdmin's "who can get into this tenant" list. No hash, ever.
public class TenantUserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsSuperAdmin { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    /// Null until the user signs in for the first time after this field was introduced.
    public DateTime? LastLoginAt { get; set; }
}
