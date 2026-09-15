using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// W1·3 moduli (ishlab chiqarish + sifat nazorati) — egasi shu modul agenti.

internal sealed class ProductionStageConfiguration : IEntityTypeConfiguration<ProductionStage>
{
    public void Configure(EntityTypeBuilder<ProductionStage> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}

internal sealed class ProductionRecipeConfiguration : IEntityTypeConfiguration<ProductionRecipe>
{
    public void Configure(EntityTypeBuilder<ProductionRecipe> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(r => r.OutputProduct).WithMany().HasForeignKey(r => r.OutputProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.OutputUnit).WithMany().HasForeignKey(r => r.OutputUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RecipeStageConfiguration : IEntityTypeConfiguration<RecipeStage>
{
    public void Configure(EntityTypeBuilder<RecipeStage> builder)
    {
        builder.HasOne(rs => rs.Recipe).WithMany(r => r.RecipeStages).HasForeignKey(rs => rs.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rs => rs.Stage).WithMany().HasForeignKey(rs => rs.StageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rs => rs.OutputProduct).WithMany().HasForeignKey(rs => rs.OutputProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(rs => rs.OutputWarehouse).WithMany().HasForeignKey(rs => rs.OutputWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RecipeStageItemConfiguration : IEntityTypeConfiguration<RecipeStageItem>
{
    public void Configure(EntityTypeBuilder<RecipeStageItem> builder)
    {
        builder.HasOne(i => i.RecipeStage).WithMany(rs => rs.Inputs).HasForeignKey(i => i.RecipeStageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Unit).WithMany().HasForeignKey(i => i.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.Property(o => o.Note).HasMaxLength(1000);
        builder.HasOne(o => o.Recipe).WithMany().HasForeignKey(o => o.RecipeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.AssignedToUser).WithMany().HasForeignKey(o => o.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => new { o.TenantId, o.Status });

        // Qisqa raqam tenant ichida noyob (Transfer bilan bir xil naqsh).
        builder.HasIndex(o => new { o.TenantId, o.Number }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class StageExecutionConfiguration : IEntityTypeConfiguration<StageExecution>
{
    public void Configure(EntityTypeBuilder<StageExecution> builder)
    {
        builder.Property(e => e.Note).HasMaxLength(1000);
        builder.HasOne(e => e.ProductionOrder).WithMany(o => o.StageExecutions).HasForeignKey(e => e.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.RecipeStage).WithMany().HasForeignKey(e => e.RecipeStageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.WorkerUser).WithMany().HasForeignKey(e => e.WorkerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QcParameterConfiguration : IEntityTypeConfiguration<QcParameter>
{
    public void Configure(EntityTypeBuilder<QcParameter> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Unit).HasMaxLength(32);
    }
}

internal sealed class QcCheckConfiguration : IEntityTypeConfiguration<QcCheck>
{
    public void Configure(EntityTypeBuilder<QcCheck> builder)
    {
        builder.Property(c => c.Value).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Note).HasMaxLength(1000);
        builder.HasOne(c => c.StageExecution).WithMany(e => e.QcChecks).HasForeignKey(c => c.StageExecutionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Transfer).WithMany().HasForeignKey(c => c.TransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Parameter).WithMany().HasForeignKey(c => c.ParameterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.CheckedByUser).WithMany().HasForeignKey(c => c.CheckedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
