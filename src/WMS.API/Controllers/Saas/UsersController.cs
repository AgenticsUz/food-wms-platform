using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Saas;

/// <summary>
/// Zavod foydalanuvchilari — WMS'dagi qismi: ro'yxat, rollar, WMS o'chirgichi.
/// </summary>
/// <remarks>
/// F6 (D5, D7): <c>POST /api/users</c> (yaratish), <c>DELETE /api/users/{id}</c> va
/// <c>POST /api/users/{id}/reset-password</c> O'CHDI. Hisob, parol va bloklash Identity'da; odamni
/// zavodga Console biriktiradi va profil birinchi tokenda JIT yoziladi. Identity'dagi bloklash
/// butun platformaga, bu yerdagi <c>isActive</c> esa faqat shu zavodning WMS'iga ta'sir qiladi.
/// </remarks>
[RequirePermission(WmsPermissions.SettingsUsers)]
public class UsersController : BaseController
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    /// <remarks>
    /// Ro'yxat boshqa ekranlarda ham xodim tanlash uchun kerak: KPI davomati (`kpi.manage`) va
    /// ishlab chiqarish buyurtmasining mas'uli (`production.manage`).
    /// </remarks>
    [HttpGet]
    [RequireAnyPermission(WmsPermissions.SettingsUsers, WmsPermissions.KpiManage, WmsPermissions.ProductionManage)]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll(CancellationToken ct)
        => Ok(ApiResponse<List<UserDto>>.Ok(await _users.GetAllAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Get(Guid id, CancellationToken ct)
        => Ok(ApiResponse<UserDto>.Ok(await _users.GetByIdAsync(id, ct)));

    /// <summary>Faqat <c>{ isActive }</c> — ism va telefon Identity'dan keladi (DTO izohi).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
        => Ok(ApiResponse<UserDto>.Ok(await _users.UpdateAsync(id, dto, CurrentUser.ProfileId, ct)));

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<ApiResponse<UserDto>>> AssignRoles(Guid id, [FromBody] AssignRolesDto dto, CancellationToken ct)
        => Ok(ApiResponse<UserDto>.Ok(await _users.AssignRolesAsync(id, dto, ct), "Roles assigned"));
}

/// <summary>
/// Rollar ekrani. Tizim rollari (<c>admin/manager/employee/viewer</c>) tahrirlanadi, lekin o'chirilmaydi.
/// </summary>
[Route("api/roles")]
[RequirePermission(WmsPermissions.SettingsRoles)]
public class RolesController : BaseController
{
    private readonly IUserService _users;
    public RolesController(IUserService users) => _users = users;

    /// <summary>Rollar ruxsat kodlari va foydalanuvchilar soni bilan — ekran bitta so'rovda chiziladi.</summary>
    /// <remarks>Foydalanuvchilar ekrani rol biriktirish dialogida shu ro'yxatni o'qiydi — `settings.users` ham yetadi.</remarks>
    [HttpGet]
    [RequireAnyPermission(WmsPermissions.SettingsRoles, WmsPermissions.SettingsUsers)]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAll(CancellationToken ct)
        => Ok(ApiResponse<List<RoleDto>>.Ok(await _users.GetRolesAsync(ct)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleDto dto, CancellationToken ct)
        => Ok(ApiResponse<RoleDto>.Ok(await _users.CreateRoleAsync(dto, ct)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(Guid id, [FromBody] UpdateRoleDto dto, CancellationToken ct)
        => Ok(ApiResponse<RoleDto>.Ok(await _users.UpdateRoleAsync(id, dto, ct)));

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _users.DeleteRoleAsync(id, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Deleted"));
    }

    [HttpGet("{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissions(Guid id, CancellationToken ct)
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _users.GetRolePermissionsAsync(id, ct)));

    /// <summary><c>{ permissionCodes: [...] }</c> — ro'yxat rolning to'plamini ALMASHTIRADI.</summary>
    [HttpPut("{id:guid}/permissions")]
    public async Task<ActionResult<ApiResponse<object>>> AssignPermissions(Guid id, [FromBody] AssignPermissionsDto? dto, CancellationToken ct)
    {
        await _users.AssignPermissionsAsync(id, dto ?? new AssignPermissionsDto(), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Permissions assigned"));
    }
}

/// <summary>Ruxsat katalogi (kodda — <c>WmsPermissions</c>) va har birining obunada mavjudligi.</summary>
[Route("api/permissions")]
public class PermissionsController : BaseController
{
    private readonly IUserService _users;
    public PermissionsController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetAll(CancellationToken ct)
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _users.GetAllPermissionsAsync(ct)));
}
