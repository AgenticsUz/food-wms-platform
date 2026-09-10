using Microsoft.EntityFrameworkCore;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.DTOs.Users;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Saas;

/// <inheritdoc />
/// <remarks>
/// Hamma so'rov joriy tenant kontekstida: global filtr + RLS (D4) — SQLite davridagi
/// <c>TenantId == tenantId</c> shartlari o'chdi. Console (<c>/admin/v1/tenants/{id}/users</c>) ham
/// <see cref="GetAllAsync"/> ni kontekstni o'sha tenantga qo'yib chaqiradi.
/// </remarks>
public class UserService : IUserService
{
    private readonly WmsDbContext _db;
    private readonly IRequestWarnings _warnings;
    private readonly IWmsAccessResolver _access;
    private readonly ITenantStateService _tenantState;
    private readonly ICurrentTenant _currentTenant;

    public UserService(WmsDbContext db, IRequestWarnings warnings, IWmsAccessResolver access,
        ITenantStateService tenantState, ICurrentTenant currentTenant)
    {
        _db = db;
        _warnings = warnings;
        _access = access;
        _tenantState = tenantState;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId
        ?? throw new InvalidOperationException("Foydalanuvchilar ekrani tenant kontekstisiz chaqirildi.");

    // ── Foydalanuvchilar ────────────────────────────────────────────────────

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
        => await UserQuery().OrderBy(u => u.FullName).ToListAsync(ct);

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await UserQuery().FirstOrDefaultAsync(u => u.Id == id, ct)
           ?? throw new NotFoundException("User not found");

    private IQueryable<UserDto> UserQuery() => _db.UserProfiles.AsNoTracking()
        .Select(u => new UserDto
        {
            Id = u.Id, IdentitySub = u.IdentitySub, FullName = u.FullName, Phone = u.Phone,
            IsActive = u.IsActive, LastSeenAt = u.LastSeenAt,
            Roles = u.UserRoles.Select(ur => new UserRoleDto { Id = ur.Role.Id, Code = ur.Role.Code, Name = ur.Role.Name }).ToList()
        });

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto, Guid? actingProfileId, CancellationToken ct = default)
    {
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User not found");

        // O'zini o'chirgan admin ekranni yopib qo'yardi va qaytarishni faqat Console qila olardi.
        if (!dto.IsActive && actingProfileId == id)
            throw new AppException("You cannot deactivate your own account");

        user.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);

        // Resolver faol bo'lmagan profilga huquq bermaydi — kesh bekor bo'lmasa o'chirilgan odam
        // yana 5 daqiqa ishlab turardi.
        _access.Invalidate(user.IdentitySub, TenantId);
        return await GetByIdAsync(id, ct);
    }

    public async Task<UserDto> AssignRolesAsync(Guid userId, AssignRolesDto dto, CancellationToken ct = default)
    {
        var user = await _db.UserProfiles.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found");

        // Rollar filtr orqali faqat joriy tenantdan — begona id «topilmadi».
        var roleIds = (dto.RoleIds ?? []).Distinct().ToList();
        var validCount = await _db.Roles.CountAsync(r => roleIds.Contains(r.Id), ct);
        if (validCount != roleIds.Count)
            throw new NotFoundException("Role not found");

        // Farq bo'yicha: faqat olib tashlanganlari o'chadi, faqat yangilari qo'shiladi — (user, role)
        // noyob indeksi bir SaveChanges ichidagi «o'chir-qayta qo'sh» bilan to'qnashmasin.
        foreach (var existing in user.UserRoles.Where(ur => !roleIds.Contains(ur.RoleId)).ToList())
            _db.UserRoles.Remove(existing);

        var current = user.UserRoles.Select(ur => ur.RoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(r => !current.Contains(r)))
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });

        await _db.SaveChangesAsync(ct);
        _access.Invalidate(user.IdentitySub, TenantId);
        return await GetByIdAsync(userId, ct);
    }

    // ── Rollar ──────────────────────────────────────────────────────────────

    public async Task<List<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = await _db.Roles.AsNoTracking()
            .OrderByDescending(r => r.IsSystem).ThenBy(r => r.Name)
            .Select(r => new RoleDto
            {
                Id = r.Id, Code = r.Code, Name = r.Name, Description = r.Description, IsSystem = r.IsSystem,
                UserCount = r.UserRoles.Count(),
                Permissions = r.RolePermissions.Select(rp => rp.PermissionCode).ToList()
            })
            .ToListAsync(ct);

        foreach (var role in roles) role.Permissions.Sort(StringComparer.Ordinal);
        return roles;
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default)
    {
        var (name, description) = ValidateRole(dto.Name, dto.Description);
        await EnsureUniqueNameAsync(name, null, ct);

        var role = new Role { Name = name, Description = description };
        foreach (var code in KnownCodes(dto.PermissionCodes))
            role.RolePermissions.Add(new RolePermission { PermissionCode = code });

        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        await WarnOutsidePlanAsync(role.RolePermissions.Select(rp => rp.PermissionCode), ct);

        return await GetRoleAsync(role.Id, ct);
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role not found");

        // Tizim roli ham qayta nomlanadi — JIT uni NOMI bilan emas, KODI bilan topadi.
        var (name, description) = ValidateRole(dto.Name, dto.Description);
        await EnsureUniqueNameAsync(name, id, ct);

        role.Name = name;
        role.Description = description;
        await _db.SaveChangesAsync(ct);
        return await GetRoleAsync(id, ct);
    }

    public async Task DeleteRoleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role not found");

        // JIT yangi odamni Identity'dagi yirik rol bo'yicha aynan shu tizim roliga biriktiradi.
        if (role.IsSystem)
            throw new AppException("System roles cannot be deleted");

        // ⚠️ Bog'lanishlar QATTIQ o'chadi: resolver `user_role → role_permission` ni rolning o'zini
        // tekshirmay join qiladi — SQLite davridagidek faqat rolni yumshoq o'chirish uning ruxsatlarini
        // biriktirilgan odamlarda jimgina ishlatib qoldirardi.
        _db.UserRoles.RemoveRange(await _db.UserRoles.Where(ur => ur.RoleId == id).ToListAsync(ct));
        _db.RolePermissions.RemoveRange(await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(ct));
        role.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        _access.InvalidateTenant(TenantId);
    }

    // ── Ruxsatlar ───────────────────────────────────────────────────────────

    /// <summary>
    /// Katalog **kesilmaydi** — har ruxsat `IsAvailable` bilan qaytadi. Sababi: Basic
    /// obunali mijoz "Ishlab chiqarish" ruxsatlarini ko'rmasa, nima uchun texnologda
    /// menyu yo'qligini tushunmaydi. Ko'rinib tursa-yu kulrang bo'lsa — sabab ham
    /// ravshan, obunani kengaytirish taklifi ham o'z-o'zidan chiqadi.
    /// </summary>
    public async Task<List<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        var enabled = await EnabledModulesAsync(ct);
        return WmsPermissions.All
            .OrderBy(p => p.Group, StringComparer.Ordinal).ThenBy(p => p.Code, StringComparer.Ordinal)
            .Select(p => ToDto(p, enabled))
            .ToList();
    }

    public async Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct = default)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId, ct))
            throw new NotFoundException("Role not found");

        var codes = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionCode).ToListAsync(ct);
        var enabled = await EnabledModulesAsync(ct);

        // Katalogda yo'q kod (eski yozuv) ko'rsatilmaydi — u baribir hech narsa ochmaydi.
        return WmsPermissions.All
            .Where(p => codes.Contains(p.Code, StringComparer.Ordinal))
            .Select(p => ToDto(p, enabled))
            .ToList();
    }

    public async Task AssignPermissionsAsync(Guid roleId, AssignPermissionsDto dto, CancellationToken ct = default)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == roleId, ct))
            throw new NotFoundException("Role not found");

        // Noma'lum kod jimgina tashlanadi (SQLite davridagidek) — katalogdan tashqari yozuv hech narsa ochmasdi.
        var wanted = KnownCodes(dto.PermissionCodes).ToHashSet(StringComparer.Ordinal);
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);

        // Qattiq o'chirish ataylab (StampEntries izohi): yumshoq qator (role_id, permission_code) noyob
        // indeksini keyingi qayta berishda to'qnashtirardi.
        _db.RolePermissions.RemoveRange(existing.Where(rp => !wanted.Contains(rp.PermissionCode)));
        foreach (var code in wanted.Where(c => existing.All(rp => rp.PermissionCode != c)))
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionCode = code });

        await _db.SaveChangesAsync(ct);

        // Rolning ruxsatlari tenantdagi har kimga ta'sir qiladi — hammaning keshi bekor.
        _access.InvalidateTenant(TenantId);

        // Tarifga kirmaydigan ruxsatlar **saqlanadi**, jimgina tashlanmaydi: mijoz obunani
        // keyin kengaytirsa rol allaqachon to'g'ri sozlangan bo'ladi. Gate baribir 403
        // beradi, shuning uchun bu xavfsizlik masalasi emas. Lekin admin nima
        // saqlanganini bilishi kerak — shu sababli ogohlantirish.
        await WarnOutsidePlanAsync(wanted, ct);
    }

    // ── Yordamchilar ────────────────────────────────────────────────────────

    private async Task<RoleDto> GetRoleAsync(Guid id, CancellationToken ct)
        => (await GetRolesAsync(ct)).First(r => r.Id == id);

    private static (string Name, string? Description) ValidateRole(string? name, string? description)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) throw new AppException("Role name is required");
        if (trimmed.Length > 100) throw new AppException("Role name must be at most {0} characters", 100);

        var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (desc is { Length: > 500 }) throw new AppException("Role description must be at most {0} characters", 500);
        return (trimmed, desc);
    }

    /// Bir xil nomli ikki rol foydalanuvchiga rol berishda qaysi biri ekanini ajratib bo'lmas qilardi.
    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var lower = name.ToLower();
        if (await _db.Roles.AnyAsync(r => r.Name.ToLower() == lower && r.Id != exceptId, ct))
            throw new AppException("A role with this name already exists");
    }

    private static IEnumerable<string> KnownCodes(IEnumerable<string>? codes)
        => (codes ?? []).Select(c => c?.Trim() ?? "").Where(WmsPermissions.IsKnown).Distinct(StringComparer.Ordinal);

    private async Task WarnOutsidePlanAsync(IEnumerable<string> codes, CancellationToken ct)
    {
        var enabled = await EnabledModulesAsync(ct);
        var outside = codes.Count(c => !PermissionModules.IsCodeAvailable(c, enabled));
        if (outside > 0)
            _warnings.Add("permissions_outside_plan", Messages.PermissionsOutsidePlan, outside);
    }

    /// Tenantda yoqilgan modullar — Identity obunasidan (D6), keshlangan holatdan.
    private async Task<IReadOnlySet<string>> EnabledModulesAsync(CancellationToken ct)
    {
        var state = await _tenantState.GetAsync(TenantId, ct);
        return state?.EnabledModules ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static PermissionDto ToDto(PermissionDefinition p, IReadOnlySet<string> enabled) => new()
    {
        Code = p.Code, Name = p.Name, Module = p.Group,
        IsAvailable = PermissionModules.IsAvailable(p.Group, enabled)
    };
}
