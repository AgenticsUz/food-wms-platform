using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly WmsDbContext _db;
    private readonly IRequestWarnings _warnings;
    private readonly SubscriptionOptions _subscription;
    private readonly IUserSecurityService _security;

    public UserService(WmsDbContext db, IRequestWarnings warnings,
        Microsoft.Extensions.Options.IOptions<SubscriptionOptions> subscription,
        IUserSecurityService security)
    {
        _db = db;
        _warnings = warnings;
        _subscription = subscription.Value;
        _security = security;
    }

    public async Task<List<UserDto>> GetAllAsync(int tenantId)
    {
        return await _db.Users
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id, FullName = u.FullName, Phone = u.Phone, IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => new UserRoleDto { Id = ur.Role.Id, Name = ur.Role.Name }).ToList()
            }).ToListAsync();
    }

    public async Task<UserDto> CreateAsync(int tenantId, CreateUserDto dto)
    {
        await PlanLimits.EnsureCanAddUserAsync(_db, tenantId);

        var phone = PhoneHelper.Normalize(dto.Phone);
        if (await _db.Users.AnyAsync(u => u.TenantId == tenantId && u.Phone == phone))
            throw new AppException("A user with this phone already exists");

        var user = new User
        {
            TenantId = tenantId, FullName = dto.FullName, Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = dto.IsActive,
            // Parolni admin qo'ydi va xodimga aytdi — xodim o'zinikini qo'yishi kerak.
            MustChangePassword = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await PlanLimits.ReportUsageAsync(_db, _warnings, tenantId, PlanLimits.Users,
            _subscription.LimitWarnPercent);

        return new UserDto { Id = user.Id, FullName = user.FullName, Phone = user.Phone, IsActive = user.IsActive };
    }

    public async Task<UserDto> UpdateAsync(int tenantId, int id, UpdateUserDto dto)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");
        user.FullName = dto.FullName;
        user.Phone = PhoneHelper.Normalize(dto.Phone);
        user.IsActive = dto.IsActive;
        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            // Same rule as a reset: a new password ends the old sessions.
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            _security.Invalidate(user.Id);
        }
        await _db.SaveChangesAsync();
        return new UserDto
        {
            Id = user.Id, FullName = user.FullName, Phone = user.Phone, IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => new UserRoleDto { Id = ur.Role.Id, Name = ur.Role.Name }).ToList()
        };
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");
        user.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task AssignRolesAsync(int tenantId, int userId, AssignRolesDto dto)
    {
        var user = await _db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        // Every assigned role must belong to the caller's tenant.
        var roleIds = dto.RoleIds.Distinct().ToList();
        var validCount = await _db.Roles.CountAsync(r => r.TenantId == tenantId && roleIds.Contains(r.Id));
        if (validCount != roleIds.Count)
            throw new NotFoundException("Role not found");

        // Remove existing (hard delete for join table)
        _db.UserRoles.RemoveRange(user.UserRoles);

        foreach (var roleId in roleIds)
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });

        await _db.SaveChangesAsync();
    }

    public async Task<List<RoleDto>> GetRolesAsync(int tenantId)
    {
        return await _db.Roles.Where(r => r.TenantId == tenantId)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync();
    }

    public async Task<RoleDto> CreateRoleAsync(int tenantId, CreateRoleDto dto)
    {
        var role = new Role { TenantId = tenantId, Name = dto.Name, Description = dto.Description };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task<RoleDto> UpdateRoleAsync(int tenantId, int id, UpdateRoleDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new NotFoundException("Role not found");
        role.Name = dto.Name;
        role.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task DeleteRoleAsync(int tenantId, int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new NotFoundException("Role not found");
        role.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Katalog **kesilmaydi** — har ruxsat `IsAvailable` bilan qaytadi. Sababi: Basic
    /// planli mijoz "Ishlab chiqarish" ruxsatlarini ko'rmasa, nima uchun texnologda
    /// menyu yo'qligini tushunmaydi. Ko'rinib tursa-yu kulrang bo'lsa — sabab ham
    /// ravshan, tarifni kengaytirish taklifi ham o'z-o'zidan chiqadi.
    /// </summary>
    public async Task<List<PermissionDto>> GetAllPermissionsAsync(int tenantId)
    {
        var enabled = await EnabledModuleCodesAsync(tenantId);

        var all = await _db.Permissions
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Id = p.Id, Code = p.Code, Name = p.Name,
                Module = p.Module, Description = p.Description
            }).ToListAsync();

        foreach (var p in all)
            p.IsAvailable = PermissionModules.IsAvailable(p.Module, enabled);

        return all;
    }

    /// Tenantda yoqilgan modul kodlari. Plansiz tenant — barcha modullar
    /// (`TenantModule` qatorlari shu holatni allaqachon aks ettiradi).
    private async Task<HashSet<string>> EnabledModuleCodesAsync(int tenantId)
        => (await _db.TenantModules
                .Where(tm => tm.TenantId == tenantId && tm.IsEnabled)
                .Join(_db.Modules, tm => tm.ModuleId, m => m.Id, (tm, m) => m.Code)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(int tenantId, int roleId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId)
            ?? throw new NotFoundException("Role not found");

        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id, Code = rp.Permission.Code, Name = rp.Permission.Name,
                Module = rp.Permission.Module, Description = rp.Permission.Description
            }).ToListAsync();
    }

    public async Task AssignPermissionsAsync(int tenantId, int roleId, AssignPermissionsDto dto)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId)
            ?? throw new NotFoundException("Role not found");

        // Remove existing (hard delete for join table)
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        var requestedIds = dto.PermissionIds.Where(x => x > 0).Distinct().ToList();
        var valid = await _db.Permissions
            .Where(p => requestedIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Module })
            .ToListAsync();
        foreach (var perm in valid)
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = perm.Id });

        await _db.SaveChangesAsync();

        // Tarifga kirmaydigan ruxsatlar **saqlanadi**, jimgina tashlanmaydi: mijoz planni
        // keyin kengaytirsa rol allaqachon to'g'ri sozlangan bo'ladi. Gate baribir 403
        // beradi, shuning uchun bu xavfsizlik masalasi emas. Lekin admin nima
        // saqlanganini bilishi kerak — shu sababli ogohlantirish.
        var enabled = await EnabledModuleCodesAsync(tenantId);
        var outsidePlan = valid.Count(p => !PermissionModules.IsAvailable(p.Module, enabled));
        if (outsidePlan > 0)
            _warnings.Add("permissions_outside_plan", Messages.PermissionsOutsidePlan, outsidePlan);
    }

    public async Task<List<string>> GetUserPermissionsAsync(int tenantId, int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new NotFoundException("User not found");

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        return await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();
    }
}
