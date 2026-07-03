using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.Common;
using WMS.Application.DTOs.Auth;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly WmsDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(WmsDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == dto.TenantSlug && t.IsActive)
            ?? throw new NotFoundException("Tenant not found or inactive");

        var normalizedPhone = PhoneHelper.Normalize(dto.Phone);
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Phone == normalizedPhone && u.IsActive)
            ?? throw new AppException("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new AppException("Invalid credentials");

        // Subscription enforcement — super admins bypass. Block suspended tenants and
        // trials whose TrialEndsAt has passed.
        if (!user.IsSuperAdmin &&
            (tenant.SubscriptionStatus == SubscriptionStatus.Suspended ||
             (tenant.SubscriptionStatus == SubscriptionStatus.Trial &&
              tenant.TrialEndsAt != null && tenant.TrialEndsAt < DateTime.UtcNow)))
            throw new AppException("This account is suspended or the trial has expired. Please contact support.");

        var modules = await _db.TenantModules
            .Include(tm => tm.Module)
            .Where(tm => tm.TenantId == tenant.Id && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var roleName = roles.FirstOrDefault() ?? "User";

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        var claims = new List<Claim>
        {
            new("tenantId", tenant.Id.ToString()),
            new("userId", user.Id.ToString()),
            new("fullName", user.FullName),
            new(ClaimTypes.Role, roleName)
        };
        if (user.IsSuperAdmin)
            claims.Add(new Claim("isSuperAdmin", "true"));

        return new AuthResponseDto
        {
            Token = GenerateToken(claims),
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Phone = user.Phone,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                IsSuperAdmin = user.IsSuperAdmin,
                Roles = roles,
                EnabledModules = modules,
                Permissions = permissions
            }
        };
    }

    private string GenerateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Yangi tenantni to'liq provizatsiya qilamiz (tenant + modullar + Admin rol + admin user).
        // Slug/parol validatsiyasi va noyoblik tekshiruvi provisioner ichida.
        var (tenant, user) = await TenantProvisioner.ProvisionAsync(
            _db, dto.TenantName, dto.Slug, dto.FullName, dto.Phone, dto.Password);

        // Admin darhol tizimga kiradi — login javobini qaytaramiz.
        var modules = await _db.Modules.Select(m => m.Code).ToListAsync();
        var permissions = await _db.Permissions.Select(p => p.Code).ToListAsync();

        var claims = new List<Claim>
        {
            new("tenantId", tenant.Id.ToString()),
            new("userId", user.Id.ToString()),
            new("fullName", user.FullName),
            new(ClaimTypes.Role, "Admin")
        };

        return new AuthResponseDto
        {
            Token = GenerateToken(claims),
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Phone = user.Phone,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                IsSuperAdmin = false,
                Roles = new List<string> { "Admin" },
                EnabledModules = modules,
                Permissions = permissions
            }
        };
    }

    public async Task<UserInfoDto> GetCurrentUserAsync(int userId, int tenantId)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        var modules = await _db.TenantModules
            .Include(tm => tm.Module)
            .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        return new UserInfoDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone,
            TenantId = tenantId,
            TenantName = user.Tenant.Name,
            IsSuperAdmin = user.IsSuperAdmin,
            TelegramChatId = user.TelegramChatId,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            EnabledModules = modules,
            Permissions = permissions
        };
    }

    public async Task UpdateProfileAsync(int userId, int tenantId, UpdateProfileDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        var normalizedPhone = PhoneHelper.Normalize(dto.Phone);
        var phoneTaken = await _db.Users.AnyAsync(u =>
            u.TenantId == tenantId && u.Id != userId && u.Phone == normalizedPhone);
        if (phoneTaken) throw new AppException("Phone number already belongs to another user");

        user.FullName = dto.FullName;
        user.Phone = normalizedPhone;
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int userId, int tenantId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new AppException("Current password is incorrect");

        if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new AppException("New password must be at least 6 characters");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
    }

    public async Task SetTelegramChatAsync(int userId, int tenantId, SetTelegramDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        var chatId = dto.ChatId?.Trim();
        user.TelegramChatId = string.IsNullOrWhiteSpace(chatId) ? null : chatId;
        await _db.SaveChangesAsync();
    }
}
