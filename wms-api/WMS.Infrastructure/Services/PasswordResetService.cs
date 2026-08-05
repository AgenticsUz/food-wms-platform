using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class PasswordResetService : IPasswordResetService
{
    private readonly WmsDbContext _db;
    private readonly IUserSecurityService _security;

    public PasswordResetService(WmsDbContext db, IUserSecurityService security)
    {
        _db = db;
        _security = security;
    }

    public async Task<PasswordResetResultDto> ResetByPlatformAsync(int tenantId, ResetUserPasswordDto dto,
        int actorUserId, CancellationToken ct = default)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            throw new NotFoundException("Tenant not found");

        var user = dto.UserId is { } userId
            ? await FindInTenantAsync(tenantId, userId, ct)
            : await FindTenantAdminAsync(tenantId, ct);

        return await ApplyAsync(user, dto.NewPassword, ct);
    }

    public async Task<PasswordResetResultDto> ResetByTenantAdminAsync(int tenantId, int targetUserId,
        ResetPasswordDto dto, int actorUserId, CancellationToken ct = default)
    {
        var user = await FindInTenantAsync(tenantId, targetUserId, ct);

        // A tenant admin never touches a platform account, even one that happens to sit in
        // their tenant — that is how someone would take over the control plane.
        if (user.IsSuperAdmin)
            throw new ForbiddenException(Messages.CannotResetPlatformUser);

        // Resetting your own password this way would skip the "current password" check.
        if (user.Id == actorUserId)
            throw new ForbiddenException(Messages.UseChangePasswordInstead);

        return await ApplyAsync(user, dto.NewPassword, ct);
    }

    public async Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId, CancellationToken ct = default)
    {
        if (!await _db.Tenants.AnyAsync(t => t.Id == tenantId, ct))
            throw new NotFoundException("Tenant not found");

        // The hash never leaves the database — not even to a SuperAdmin.
        return await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id)
            .Select(u => new TenantUserDto
            {
                Id = u.Id,
                Login = u.Phone,
                FullName = u.FullName,
                IsActive = u.IsActive,
                IsSuperAdmin = u.IsSuperAdmin,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);
    }

    /// <summary>
    /// Sets the password, rotates the security stamp (which invalidates every token issued
    /// before now) and drops the cached stamp so the very next request with an old token
    /// fails rather than lingering until the cache expires.
    /// </summary>
    private async Task<PasswordResetResultDto> ApplyAsync(User user, string? requested, CancellationToken ct)
    {
        var password = string.IsNullOrWhiteSpace(requested)
            ? PasswordGenerator.Generate()
            : requested.Trim();

        if (password.Length < PasswordGenerator.MinimumManualLength)
            throw new AppException(Messages.PasswordTooShort, PasswordGenerator.MinimumManualLength);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);
        _security.Invalidate(user.Id);

        return new PasswordResetResultDto
        {
            UserId = user.Id,
            Login = user.Phone,
            FullName = user.FullName,
            NewPassword = password
        };
    }

    /// Soft-deleted users are filtered out globally, and a user from another tenant is
    /// reported as missing rather than as forbidden — the caller has no business knowing
    /// that the id exists at all.
    private async Task<User> FindInTenantAsync(int tenantId, int userId, CancellationToken ct)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct)
           ?? throw new NotFoundException("User not found");

    /// "The tenant's admin": the oldest active user holding a role that grants
    /// settings.users. Falls back to the oldest active user, because a tenant whose roles
    /// were reshuffled still needs a way back in.
    private async Task<User> FindTenantAdminAsync(int tenantId, CancellationToken ct)
    {
        var admin = await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .Where(u => u.UserRoles.Any(ur =>
                _db.RolePermissions.Any(rp => rp.RoleId == ur.RoleId && rp.Permission.Code == "settings.users")))
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(ct);

        admin ??= await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(ct);

        return admin ?? throw new NotFoundException("User not found");
    }
}
