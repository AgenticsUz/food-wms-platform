using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Yangi tenantni to'liq provizatsiya qiladi: Tenant + barcha modullar + "Admin" rol
/// (barcha ruxsatlar bilan) + admin foydalanuvchi. Ham self-service registratsiya, ham
/// SuperAdmin "tenant yaratish" shu yagona joydan foydalanadi (kod takrorlanmasin).
/// </summary>
public static class TenantProvisioner
{
    // Zaxira slug'lar — tizim yo'llari bilan to'qnashmasin / adashtirmasin.
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "www", "app", "auth", "login", "register", "portal",
        "agent-portal", "superadmin", "super-admin", "settings", "dashboard",
        "system", "root", "support", "help", "static", "assets", "public",
        "billing", "payment", "webhook", "health", "status"
    };

    public static async Task<(Tenant tenant, User admin)> ProvisionAsync(
        WmsDbContext db, string tenantName, string slug,
        string adminFullName, string adminPhone, string adminPassword)
    {
        slug = slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(tenantName)) throw new AppException("Tenant name is required");
        if (string.IsNullOrWhiteSpace(slug)) throw new AppException("Slug is required");
        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9](?:[a-z0-9-]{1,48}[a-z0-9])?$"))
            throw new AppException("Slug may only contain lowercase letters, digits and hyphens (3-50 chars)");
        if (ReservedSlugs.Contains(slug))
            throw new AppException("This slug is reserved, please choose another");
        if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 6)
            throw new AppException("Admin password must be at least 6 characters");

        if (await db.Tenants.AnyAsync(t => t.Slug == slug))
            throw new AppException("This slug is already taken");

        var normalizedPhone = PhoneHelper.Normalize(adminPhone)
            ?? throw new AppException("Phone is required");

        // 1. Tenant (trial obuna bilan boshlaydi)
        var tenant = new Tenant
        {
            Name = tenantName.Trim(),
            Slug = slug,
            IsActive = true,
            PlanType = "trial",
            SubscriptionStatus = SubscriptionStatus.Trial
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 2. Barcha modullarni yoqamiz
        var moduleIds = await db.Modules.Select(m => m.Id).ToListAsync();
        foreach (var moduleId in moduleIds)
            db.TenantModules.Add(new TenantModule { TenantId = tenant.Id, ModuleId = moduleId, IsEnabled = true });

        // 3. Admin rol + barcha ruxsatlar
        var role = new Role { TenantId = tenant.Id, Name = "Admin", Description = "Tenant administrator" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var permissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        foreach (var permissionId in permissionIds)
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

        // 4. Admin foydalanuvchi
        var user = new User
        {
            TenantId = tenant.Id,
            FullName = adminFullName.Trim(),
            Phone = normalizedPhone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        return (tenant, user);
    }
}
