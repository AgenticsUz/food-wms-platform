using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[RequirePermission("settings.users")]
public class UsersController : BaseController
{
    private readonly IUserService _users;
    private readonly IPasswordResetService _passwords;
    public UsersController(IUserService users, IPasswordResetService passwords)
    { _users = users; _passwords = passwords; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<UserDto>>.Ok(await _users.GetAllAsync(TenantId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        => Ok(ApiResponse<UserDto>.Ok(await _users.CreateAsync(TenantId, dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        => Ok(ApiResponse<UserDto>.Ok(await _users.UpdateAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _users.DeleteAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    /// <summary>
    /// A tenant admin resetting one of their own staff — the everyday case of someone
    /// forgetting their password. Same rules as the platform reset: generated or chosen,
    /// returned exactly once, audited. Never applies to a platform account.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto? dto,
        CancellationToken ct)
        => Ok(ApiResponse<PasswordResetResultDto>.Ok(
            await _passwords.ResetByTenantAdminAsync(TenantId, id, dto ?? new ResetPasswordDto(), UserId, ct),
            "Password reset"));

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesDto dto)
    { await _users.AssignRolesAsync(TenantId, id, dto); return Ok(ApiResponse<object>.Ok(null!, "Roles assigned")); }
}

[ApiController]
[Route("api/roles")]
[RequirePermission("settings.roles")]
public class RolesController : BaseController
{
    private readonly IUserService _users;
    public RolesController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<RoleDto>>.Ok(await _users.GetRolesAsync(TenantId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
        => Ok(ApiResponse<RoleDto>.Ok(await _users.CreateRoleAsync(TenantId, dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleDto dto)
        => Ok(ApiResponse<RoleDto>.Ok(await _users.UpdateRoleAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _users.DeleteRoleAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetPermissions(int id)
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _users.GetRolePermissionsAsync(TenantId, id)));

    [HttpPut("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(int id, [FromBody] AssignPermissionsDto? dto)
    {
        dto ??= new AssignPermissionsDto();
        await _users.AssignPermissionsAsync(TenantId, id, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Permissions assigned"));
    }
}

[ApiController]
[Route("api/permissions")]
public class PermissionsController : BaseController
{
    private readonly IUserService _users;
    public PermissionsController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _users.GetAllPermissionsAsync()));
}
