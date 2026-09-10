using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// Platforma jadvallari: Identity nusxasi (tenant) va tijorat qatlami (plan, feature, to'lov).
// ⚠️ HasFilter ichidagi SQL YAKUNIY (snake_case) nomlarda — sababi SnakeCaseNaming izohida.

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.Code).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.IdentityStatus).HasMaxLength(16).IsRequired();
        builder.Property(t => t.Modules).HasMaxLength(1024).IsRequired();
        builder.Property(t => t.SuspendNote).HasMaxLength(1000);
        builder.Property(t => t.SuspendPublicMessage).HasMaxLength(1000);
        builder.Property(t => t.LogoUrl).HasMaxLength(512);
        builder.Property(t => t.LogoSquareUrl).HasMaxLength(512);
        builder.Property(t => t.BrandColor).HasMaxLength(7);
        builder.Ignore(t => t.ModuleCodes);

        // Kod Identity'da noyob; nusxada ham — tenantni kod bo'yicha qidiradigan joylar
        // (Console qidiruvi, demo seed) ikki qator topmasin.
        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasOne(t => t.Plan).WithMany().HasForeignKey(t => t.PlanId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.FeatureCodes).HasMaxLength(4000).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.Property(f => f.Code).HasMaxLength(64).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(1000);
        builder.Property(f => f.ModuleCode).HasMaxLength(32);
        builder.Property(f => f.Reason).HasMaxLength(1000);
        builder.HasIndex(f => f.Code).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.Property(tf => tf.FeatureCode).HasMaxLength(64).IsRequired();
        builder.Property(tf => tf.Note).HasMaxLength(500);
        builder.HasOne(tf => tf.Tenant).WithMany().HasForeignKey(tf => tf.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(tf => new { tf.TenantId, tf.FeatureCode }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.HasOne(p => p.Tenant).WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.TenantId, p.PeriodEnd });
    }
}
