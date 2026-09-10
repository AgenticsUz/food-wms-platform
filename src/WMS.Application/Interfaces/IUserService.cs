using WMS.Application.DTOs.Users;

namespace WMS.Application.Interfaces;

/// <summary>
/// Foydalanuvchilar va rollar ekrani — JORIY tenant kontekstida (D4: tenant parametri yo'q).
/// </summary>
/// <remarks>
/// <para>
/// F6 (D5, D7): yaratish, o'chirish va parol amallari O'CHDI — hisob Identity'da, odamni zavodga
/// Console biriktiradi, profilni JIT yozadi. WMS'da: ro'yxat, WMS rollari, WMS o'chirgichi.
/// </para>
/// <para>
/// ⚠️ Rol yoki ruxsatni o'zgartiradigan HAR metod <c>IWmsAccessResolver</c> keshini bekor qiladi —
/// aks holda o'zgarish 5 daqiqalik kesh tugaguncha kuchga kirmasdi.
/// </para>
/// </remarks>
public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <param name="actingProfileId">Amalni bajarayotgan profil — o'zini o'chirib qo'yishdan saqlash uchun.</param>
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto, Guid? actingProfileId, CancellationToken ct = default);
    Task<UserDto> AssignRolesAsync(Guid userId, AssignRolesDto dto, CancellationToken ct = default);

    Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default);
    Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid id, CancellationToken ct = default);

    Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default);
    Task AssignPermissionsAsync(Guid roleId, AssignPermissionsDto dto, CancellationToken ct = default);
}
