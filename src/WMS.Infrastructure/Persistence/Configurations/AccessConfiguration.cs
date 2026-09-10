using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// Identity nusxasi (user_profile) va WMS'ning nozik RBAC'i (role, role_permission, user_role).

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.TelegramChatId).HasMaxLength(64);

        // JIT profilni `sub` bo'yicha topadi; ikki parallel birinchi so'rov ikki profil
        // yozmasin — ikkinchisi noyoblik xatosi bilan yiqiladi va keyingi so'rovda o'tadi.
        builder.HasIndex(u => new { u.TenantId, u.IdentitySub }).IsUnique();
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Code).HasMaxLength(32);
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        // Tizim roli tenantda bitta: JIT uni `code` bo'yicha qidiradi.
        builder.HasIndex(r => new { r.TenantId, r.Code }).IsUnique()
            .HasFilter("\"code\" IS NOT NULL AND \"is_deleted\" = false");
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.Property(rp => rp.PermissionCode).HasMaxLength(64).IsRequired();
        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionCode }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.UserName).HasMaxLength(200);
        builder.Property(a => a.Action).HasMaxLength(10).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityAction).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(64);
        builder.Property(a => a.Path).HasMaxLength(500).IsRequired();
        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.HasIndex(a => new { a.TenantId, a.CreatedAt });
    }
}
