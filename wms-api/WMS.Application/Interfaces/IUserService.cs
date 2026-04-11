using WMS.Application.DTOs.Users;

namespace WMS.Application.Interfaces;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(int tenantId);
    Task<UserDto> CreateAsync(int tenantId, CreateUserDto dto);
    Task<UserDto> UpdateAsync(int tenantId, int id, UpdateUserDto dto);
    Task DeleteAsync(int tenantId, int id);
    Task AssignRolesAsync(int tenantId, int userId, AssignRolesDto dto);
    Task<List<RoleDto>> GetRolesAsync(int tenantId);
    Task<RoleDto> CreateRoleAsync(int tenantId, CreateRoleDto dto);
    Task<RoleDto> UpdateRoleAsync(int tenantId, int id, UpdateRoleDto dto);
    Task DeleteRoleAsync(int tenantId, int id);
}
