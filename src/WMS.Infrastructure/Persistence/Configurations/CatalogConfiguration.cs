using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Application.Common;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// W1·1 moduli (katalog + ombor/zaxira) — egasi shu modul agenti.

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(u => u.Name).HasMaxLength(50).IsRequired();
        builder.Property(u => u.ShortName).HasMaxLength(16).IsRequired();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NameSearch).HasMaxLength(SearchNormalizer.MaxLength).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Unit).WithMany().HasForeignKey(p => p.UnitId).OnDelete(DeleteBehavior.Restrict);

        // Skaner shtrix-kodni `Enter` bilan yuboradi va mahsulot shu bo'yicha topiladi (D1).
        builder.HasIndex(p => new { p.TenantId, p.Barcode });

        // Nom bo'yicha taxminiy qidiruv (P2.1). ⚠️ Indeks `tenant_id` ni O'Z ICHIGA OLMAYDI:
        // uuid ustunini GIN ichiga qo'shish uchun `btree_gin` kengaytmasi kerak bo'lardi, u esa
        // init skriptida yo'q. Tenantni baribir RLS kesadi — indeks faqat nomzodlarni toraytiradi.
        builder.HasIndex(p => p.NameSearch).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
