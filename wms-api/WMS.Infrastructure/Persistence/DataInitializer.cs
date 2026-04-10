using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

public static class DataInitializer
{
    public static async Task SeedAsync(WmsDbContext db)
    {
        // Only seed if no tenants exist (idempotent)
        if (await db.Tenants.AnyAsync())
            return;

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
    }
}
