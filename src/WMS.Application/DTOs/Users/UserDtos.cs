namespace WMS.Application.DTOs.Users;

// F6 (D5, D7): WMS foydalanuvchi YARATMAYDI va parol bilmaydi — `CreateUserDto`, parol maydonlari,
// `IsSuperAdmin` va parol tiklash DTO'lari O'CHDI. Odamni Console Identity'da ochadi va zavodga
// biriktiradi, profil birinchi tokenda JIT yoziladi. Bu yerda — profilning WMS'dagi qismi (rollar,
// o'chirgich). Ruxsat katalogi bazada emas, kodda (`WmsPermissions`), shuning uchun ruxsat endi
// `Id` bilan emas, KOD bilan yuradi.

public class UserDto
{
    /// <c>user_profile.id</c> — WMS jadvallaridagi har <c>*UserId</c> shunga ishora qiladi.
    public Guid Id { get; set; }
    /// Identity <c>sub</c> — Console foydalanuvchini Identity'dagi hisobi bilan shu orqali bog'laydi.
    public Guid IdentitySub { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }

    /// <summary>
    /// Identity'dagi yirik rol (<c>admin</c>/<c>manager</c>/…) — JIT shu bo'yicha tizim
    /// rolini biriktiradi va uni shu ekrandan O'ZGARTIRIB BO'LMAYDI: manba «Kirish
    /// hisoblari». <see langword="null"/> — odam hali bir marta ham kirmagan.
    /// </summary>
    public string? IdentityRole { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();
}

public class UserRoleDto
{
    public Guid Id { get; set; }
    /// Tizim roli kodi (<c>admin</c>, <c>manager</c>, ...) yoki null (tenant yaratgan rol).
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
}

/// <summary>
/// <c>PUT /api/users/{id}</c> — faqat WMS o'chirgichi.
/// </summary>
/// <remarks>
/// Ism va telefon YO'Q: ular Identity'niki va JIT ularni har token yangilanishida (~15 daqiqa)
/// nusxaga qayta yozadi — WMS'da tahrirlash bir necha daqiqadan keyin jimgina bekor bo'lardi.
/// </remarks>
public class UpdateUserDto
{
    public bool IsActive { get; set; }
}

public class AssignRolesDto
{
    public List<Guid> RoleIds { get; set; } = new();
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// Tizim roli: tahrirlanadi (nom, ruxsatlar), lekin o'chirilmaydi — JIT odamlarni Identity'dagi
    /// yirik rol bo'yicha aynan shunga biriktiradi.
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// Ixtiyoriy: rol bir qadamda ruxsatlari bilan yaratilsin.
    public List<string> PermissionCodes { get; set; } = new();
}

public class UpdateRoleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class PermissionDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    /// Ruxsat guruhi (<c>WAREHOUSE</c>, <c>SETTINGS</c> ...) — UI sarlavhasi.
    public string Module { get; set; } = null!;

    /// <summary>
    /// Ruxsat tenantning obunasida amalda ishlaydimi (tegishli modul Identity'da yoqilganmi).
    /// Ro'yxatdan **olib tashlanmaydi**: frontend uni kulrang qilib "Tarifingizga
    /// kirmaydi" yorlig'i bilan ko'rsatadi. Yashirilsa, mijoz nima uchun ruxsat
    /// yo'qligini tushunmaydi va qo'ng'iroq qiladi; ko'rinib tursa — yumshoq upsell.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}

public class AssignPermissionsDto
{
    public List<string> PermissionCodes { get; set; } = new();
}
