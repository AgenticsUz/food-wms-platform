using WMS.Application.DTOs.Users;

namespace WMS.Application.Interfaces;

/// <summary>
/// Password recovery, operator-driven. There is no "forgot my password" email or SMS flow
/// yet — that needs an external provider — so the only way back into a locked-out account
/// is a person the customer phones.
/// </summary>
public interface IPasswordResetService
{
    /// SuperAdmin, on any tenant. A null userId resolves to that tenant's admin, which is
    /// the case that actually happens: the one person who could sign in cannot.
    Task<PasswordResetResultDto> ResetByPlatformAsync(int tenantId, ResetUserPasswordDto dto,
        int actorUserId, CancellationToken ct = default);

    /// Tenant admin, on their own staff only — never on a platform account.
    Task<PasswordResetResultDto> ResetByTenantAdminAsync(int tenantId, int targetUserId,
        ResetPasswordDto dto, int actorUserId, CancellationToken ct = default);

    /// SuperAdmin's view of who can sign in to a tenant.
    Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId, CancellationToken ct = default);
}

/// <summary>
/// Makes a JWT revocable without giving up stateless tokens: every user carries a security
/// stamp, the token carries a copy, and a mismatch means the token was issued before the
/// password changed.
/// </summary>
public interface IUserSecurityService
{
    /// Current stamp, or null for a user whose password has never been reset (their old
    /// tokens stay valid — nobody is logged out by deploying this).
    Task<string?> GetStampAsync(int userId, CancellationToken ct = default);

    /// Called the moment a password changes, so the next request with an old token fails
    /// immediately rather than at the end of the cache window.
    void Invalidate(int userId);
}
