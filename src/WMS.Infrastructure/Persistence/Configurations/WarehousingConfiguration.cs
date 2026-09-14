using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Application.Common;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// W1·1 moduli (katalog + ombor/zaxira) — egasi shu modul agenti.

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.NameSearch).HasMaxLength(SearchNormalizer.MaxLength).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);

        // Nom bo'yicha taxminiy qidiruv (P2.1) — sabab va cheklovlar `ProductConfiguration` da.
        builder.HasIndex(w => w.NameSearch).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Code).HasMaxLength(32);
        builder.HasOne(l => l.Warehouse).WithMany(w => w.Locations).HasForeignKey(l => l.WarehouseId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.Property(b => b.LotNumber).HasMaxLength(64).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(500);
        builder.HasOne(b => b.Product).WithMany().HasForeignKey(b => b.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(b => new { b.TenantId, b.ProductId });

        // Muddati tugayotgan partiyalar fon xizmati har sutka shu ustun bo'yicha qidiradi.
        builder.HasIndex(b => new { b.TenantId, b.ExpiryDate });
    }
}

internal sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Location).WithMany().HasForeignKey(s => s.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Batch).WithMany().HasForeignKey(s => s.BatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.TenantId, s.WarehouseId, s.ProductId });
    }
}
