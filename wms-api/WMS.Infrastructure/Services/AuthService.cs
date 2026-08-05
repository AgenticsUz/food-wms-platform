using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.Common;
using WMS.Application.DTOs.Auth;
using WMS.Application.DTOs.Branding;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly WmsDbContext _db;
    private readonly IConfiguration _config;
    private readonly SubscriptionOptions _subscription;
    private readonly IUserSecurityService _security;

    public AuthService(WmsDbContext db, IConfiguration config, IOptions<SubscriptionOptions> subscription,
        IUserSecurityService security)
    {
        _db = db;
        _config = config;
        _subscription = subscription.Value;
        _security = security;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // IsActive filtri so'rovda emas — obuna holati SubscriptionPolicy'da tekshiriladi,
        // shunda mijoz "topilmadi" o'rniga aniq sababni ko'radi.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == dto.TenantSlug)
            ?? throw new NotFoundException("Tenant not found");

        var normalizedPhone = PhoneHelper.Normalize(dto.Phone);
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Phone == normalizedPhone && u.IsActive)
            ?? throw new AppException("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new AppException("Invalid credentials");

        // Subscription enforcement — super admins bypass. Exactly the same rule the
        // per-request middleware applies (SubscriptionPolicy), so login and API access
        // can never disagree.
        if (!user.IsSuperAdmin)
        {
            var state = new TenantState
            {
                Id = tenant.Id, Name = tenant.Name, Slug = tenant.Slug,
                IsActive = tenant.IsActive, Status = tenant.SubscriptionStatus,
                TrialEndsAt = tenant.TrialEndsAt, PlanId = tenant.PlanId,
                PaidUntil = tenant.PaidUntil,
                SuspendReason = tenant.SuspendReason,
                SuspendPublicMessage = tenant.SuspendPublicMessage,
                SuspendedUntil = tenant.SuspendedUntil
            };
            var verdict = SubscriptionPolicy.Evaluate(state, _subscription, DateTime.UtcNow);
            if (!verdict.Allowed)
                throw new PaymentRequiredException(verdict.Code!, verdict.Message!);
        }

        var modules = await _db.TenantModules
            .Include(tm => tm.Module)
            .Where(tm => tm.TenantId == tenant.Id && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();
        var features = await ResolveFeatureCodesAsync(tenant.Id, modules);

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
        // Copy of the revocation stamp: a password reset rotates it and this token stops
        // validating. Users who never had a reset carry no stamp and are unaffected.
        if (!string.IsNullOrEmpty(user.SecurityStamp))
            claims.Add(new Claim("sstamp", user.SecurityStamp));

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            Token = GenerateToken(claims),
            Branding = Branding(tenant),
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
                EnabledFeatures = features,
                Permissions = permissions
            }
        };
    }

    private static BrandingDto Branding(Domain.Entities.Tenant tenant) => new()
    {
        LogoUrl = tenant.LogoUrl,
        LogoSquareUrl = tenant.LogoSquareUrl,
        BrandColor = tenant.BrandColor
    };

    /// Feature layer for the login/me responses. The frontend hides menu items by these
    /// codes; the server still enforces them with RequireFeature.
    private async Task<List<string>> ResolveFeatureCodesAsync(int tenantId, List<string> moduleCodes)
    {
        var moduleSet = new HashSet<string>(moduleCodes, StringComparer.OrdinalIgnoreCase);
        var resolved = await FeatureResolver.ResolveAsync(_db, tenantId, moduleSet);
        return resolved.Where(f => f.IsEnabled).Select(f => f.Code).ToList();
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
        // Self-service registration is off by default: tenants are created by the platform
        // owner after a conversation and a payment. The code stays because lead conversion
        // reuses exactly this provisioning path.
        if (!_config.GetValue("Registration:SelfServiceEnabled", false))
            throw new NotFoundException("Not found");

        // Yangi tenantni to'liq provizatsiya qilamiz (tenant + default plan modullari +
        // Admin rol + admin user). Slug/parol validatsiyasi provisioner ichida.
        var (tenant, user) = await TenantProvisioner.ProvisionAsync(
            _db, dto.TenantName, dto.Slug, dto.FullName, dto.Phone, dto.Password, _subscription);

        // Javob tenantning HAQIQIY huquqlaridan quriladi — global Modules/Permissions
        // jadvalidan emas. Aks holda registratsiya qilgan har kim to'liq mahsulotni ko'radi.
        var modules = await _db.TenantModules
            .Where(tm => tm.TenantId == tenant.Id && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();

        var features = await ResolveFeatureCodesAsync(tenant.Id, modules);

        var roleIds = await _db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId).ToListAsync();
        var permissions = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

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
            Branding = Branding(tenant),
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
                EnabledFeatures = features,
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

        var features = await ResolveFeatureCodesAsync(tenantId, modules);

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
            EnabledFeatures = features,
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
        // Changing your own password signs out every other session — that is the point of
        // changing it after someone else has seen it.
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync();
        _security.Invalidate(user.Id);
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
