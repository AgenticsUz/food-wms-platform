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

    /// <param name="planId">
    /// Plan chosen by the SuperAdmin. When null the platform's default (trial) plan is used —
    /// self-service registration always lands here. Only when NO plan exists at all does the
    /// tenant fall back to "everything enabled" (fresh dev database before plans are seeded).
    /// </param>
    public static async Task<(Tenant tenant, User admin)> ProvisionAsync(
        WmsDbContext db, string tenantName, string slug,
        string adminFullName, string adminPhone, string adminPassword,
        SubscriptionOptions? subscription = null, int? planId = null, string? inn = null)
    {
        subscription ??= new SubscriptionOptions();

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

        // 1. Plan — aniq berilgan yoki platformaning default (trial) plani
        var plan = planId.HasValue
            ? await db.Plans.FirstOrDefaultAsync(p => p.Id == planId.Value)
                ?? throw new NotFoundException("Plan not found")
            : await db.Plans.Where(p => p.IsActive && p.IsDefault).FirstOrDefaultAsync()
                ?? await db.Plans.Where(p => p.IsActive && p.Code == "trial").FirstOrDefaultAsync();

        // 2. Tenant. Trial muddati ALBATTA qo'yiladi — aks holda "muddati o'tgan trial"
        //    tekshiruvi hech qachon ishga tushmaydi va obuna cheksiz bepul bo'lib qoladi.
        var trialDays = plan is { TrialDays: > 0 } ? plan.TrialDays : subscription.TrialDays;
        var isTrial = plan == null || plan.TrialDays > 0 || plan.Price <= 0;

        // Platforma darajasidagi kompaniya identifikatori (S6) — STIR berilgan bo'lsa.
        var organization = await OrganizationMatcher.ResolveAsync(db, inn, tenantName, adminPhone);

        var tenant = new Tenant
        {
            OrganizationId = organization?.Id,
            Name = tenantName.Trim(),
            Slug = slug,
            IsActive = true,
            PlanId = plan?.Id,
            PlanType = plan?.Code ?? "trial",
            SubscriptionStatus = isTrial ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            TrialEndsAt = isTrial ? DateTime.UtcNow.AddDays(trialDays) : null
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 3. Modullar — plandan. Plan umuman bo'lmasa (planlar hali seed qilinmagan baza)
        //    hammasi yoqiladi, aks holda yangi o'rnatish umuman ishlamay qoladi.
        if (plan != null)
        {
            await PlanModules.ApplyPlanModulesAsync(db, tenant.Id, plan);
        }
        else
        {
            var moduleIds = await db.Modules.Select(m => m.Id).ToListAsync();
            foreach (var moduleId in moduleIds)
                db.TenantModules.Add(new TenantModule { TenantId = tenant.Id, ModuleId = moduleId, IsEnabled = true });
        }

        // 4. Admin rol + barcha ruxsatlar
        var role = new Role { TenantId = tenant.Id, Name = "Admin", Description = "Tenant administrator" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var permissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        foreach (var permissionId in permissionIds)
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

        // 5. Admin foydalanuvchi
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

        // 6. O'lchov birliklari. Bular demo ma'lumot emas — kilogramm hamma zavodda
        //    kilogramm. Ilgari faqat tizim tenantiga (DataInitializer) qo'yilardi, shuning
        //    uchun har bir yangi mijoz mahsulot kartochkasidagi bo'sh "Birlik" ro'yxatiga
        //    duch kelardi va birinchi mahsulotdan oldin ularni qo'lda kiritishi kerak edi.
        db.Units.AddRange(DefaultUnits.Select(u => new Unit
        {
            TenantId = tenant.Id, Name = u.Name, ShortName = u.ShortName
        }));
        await db.SaveChangesAsync();

        return (tenant, user);
    }

    /// Har qanday zavodda bir xil bo'lgan o'lchov birliklari — `DataInitializer` tizim
    /// tenantiga qo'yadigan to'plam bilan bir xil.
    private static readonly (string Name, string ShortName)[] DefaultUnits =
    {
        ("Kilogramm", "kg"),
        ("Litr", "litr"),
        ("Dona", "dona"),
        ("Gramm", "gramm"),
        ("Quti", "quti"),
        ("Paket", "paket")
    };
}
