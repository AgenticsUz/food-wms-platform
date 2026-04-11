using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

public static class DataInitializer
{
    public static async Task SeedAsync(WmsDbContext db)
    {
        if (await db.Tenants.AnyAsync())
        {
            // Backfill: ensure Admin role has all permissions assigned
            await EnsureAdminPermissionsAsync(db);
            return;
        }

        // 1. Create default tenant
        var tenant = new Tenant
        {
            Name = "WMS Admin",
            Slug = "admin",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 2. Assign all 9 modules to this tenant
        var modules = await db.Modules.ToListAsync();
        foreach (var module in modules)
        {
            db.TenantModules.Add(new TenantModule
            {
                TenantId = tenant.Id,
                ModuleId = module.Id,
                IsEnabled = true
            });
        }
        await db.SaveChangesAsync();

        // 3. Create default Admin role
        var role = new Role
        {
            TenantId = tenant.Id,
            Name = "Admin",
            Description = "System administrator with full access"
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        // 4. Create default admin user
        var user = new User
        {
            TenantId = tenant.Id,
            FullName = "Admin",
            Phone = "998901234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123456"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 5. Assign Admin role to the admin user
        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        // 6. Assign ALL permissions to Admin role
        await AssignAllPermissionsToRole(db, role.Id);
    }

    private static async Task EnsureAdminPermissionsAsync(WmsDbContext db)
    {
        // Find Admin roles that are missing permissions
        var adminRoles = await db.Roles.Where(r => r.Name == "Admin").ToListAsync();
        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();

        foreach (var role in adminRoles)
        {
            var existingPermIds = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingIds = allPermissionIds.Except(existingPermIds).ToList();
            if (missingIds.Count == 0) continue;

            foreach (var permId in missingIds)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task AssignAllPermissionsToRole(WmsDbContext db, int roleId)
    {
        var permissions = await db.Permissions.ToListAsync();
        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }
        await db.SaveChangesAsync();
    }
}
