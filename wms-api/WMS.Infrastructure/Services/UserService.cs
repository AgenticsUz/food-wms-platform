using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly WmsDbContext _db;
    public UserService(WmsDbContext db) => _db = db;

    public async Task<List<UserDto>> GetAllAsync(int tenantId)
    {
        return await _db.Users
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Select(u => new UserDto
            {
                Id = u.Id, FullName = u.FullName, Phone = u.Phone, IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            }).ToListAsync();
    }

    public async Task<UserDto> CreateAsync(int tenantId, CreateUserDto dto)
    {
        var user = new User
        {
            TenantId = tenantId, FullName = dto.FullName, Phone = dto.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return new UserDto { Id = user.Id, FullName = user.FullName, Phone = user.Phone, IsActive = user.IsActive };
    }

    public async Task<UserDto> UpdateAsync(int tenantId, int id, UpdateUserDto dto)
    {
        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new Exception("User not found");
        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.IsActive = dto.IsActive;
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await _db.SaveChangesAsync();
        return new UserDto
        {
            Id = user.Id, FullName = user.FullName, Phone = user.Phone, IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new Exception("User not found");
        user.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task AssignRolesAsync(int tenantId, int userId, AssignRolesDto dto)
    {
        var user = await _db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId)
            ?? throw new Exception("User not found");

        // Remove existing (hard delete for join table)
        _db.UserRoles.RemoveRange(user.UserRoles);

        foreach (var roleId in dto.RoleIds)
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
            ?? throw new Exception("Role not found");
        role.Name = dto.Name;
        role.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new RoleDto { Id = role.Id, Name = role.Name, Description = role.Description };
    }

    public async Task DeleteRoleAsync(int tenantId, int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new Exception("Role not found");
        role.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}
