using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WMS.Application.DTOs.Auth;
using WMS.Application.Interfaces;
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
            ?? throw new Exception("Tenant not found or inactive");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Phone == dto.Phone && u.IsActive)
            ?? throw new Exception("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        var modules = await _db.TenantModules
            .Include(tm => tm.Module)
            .Where(tm => tm.TenantId == tenant.Id && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var roleName = roles.FirstOrDefault() ?? "User";

        var claims = new[]
        {
            new Claim("tenantId", tenant.Id.ToString()),
            new Claim("userId", user.Id.ToString()),
            new Claim("fullName", user.FullName),
            new Claim(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            User = new UserInfoDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Phone = user.Phone,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Roles = roles,
                EnabledModules = modules
            }
        };
    }

    public async Task<UserInfoDto> GetCurrentUserAsync(int userId, int tenantId)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new Exception("User not found");

        var modules = await _db.TenantModules
            .Include(tm => tm.Module)
            .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
            .Select(tm => tm.Module.Code)
            .ToListAsync();

        return new UserInfoDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Phone = user.Phone,
            TenantId = tenantId,
            TenantName = user.Tenant.Name,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            EnabledModules = modules
        };
    }
}
