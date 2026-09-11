using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// W1·4 moduli (yetkazish + KPI/smena + bildirishnoma) — egasi shu modul agenti.

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100);
    }
}

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.Property(d => d.FullName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Phone).HasMaxLength(20);
        builder.Property(d => d.LicenseNumber).HasMaxLength(32);
    }
}

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.Property(d => d.Note).HasMaxLength(1000);
        builder.HasOne(d => d.Vehicle).WithMany().HasForeignKey(d => d.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Driver).WithMany().HasForeignKey(d => d.DriverId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.CreatedByUser).WithMany().HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(d => new { d.TenantId, d.Status });
    }
}

internal sealed class DeliveryStopConfiguration : IEntityTypeConfiguration<DeliveryStop>
{
    public void Configure(EntityTypeBuilder<DeliveryStop> builder)
    {
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Note).HasMaxLength(1000);
        builder.HasOne(s => s.Delivery).WithMany(d => d.Stops).HasForeignKey(s => s.DeliveryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Counterparty).WithMany().HasForeignKey(s => s.CounterpartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Transfer).WithMany().HasForeignKey(s => s.TransferId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder) =>
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
}

internal sealed class ShiftPlanConfiguration : IEntityTypeConfiguration<ShiftPlan>
{
    public void Configure(EntityTypeBuilder<ShiftPlan> builder)
    {
        builder.HasOne(p => p.Shift).WithMany().HasForeignKey(p => p.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Product).WithMany().HasForeignKey(p => p.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.TenantId, p.Date });
    }
}

internal sealed class ShiftActualConfiguration : IEntityTypeConfiguration<ShiftActual>
{
    public void Configure(EntityTypeBuilder<ShiftActual> builder)
    {
        builder.Property(a => a.Note).HasMaxLength(1000);
        builder.HasOne(a => a.Shift).WithMany().HasForeignKey(a => a.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Product).WithMany().HasForeignKey(a => a.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.TenantId, a.Date });
    }
}

internal sealed class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.Property(a => a.DeviceId).HasMaxLength(64);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Shift).WithMany().HasForeignKey(a => a.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.MessageTemplate).HasMaxLength(300);
        builder.Property(n => n.MessageArgs).HasMaxLength(2000);
        builder.Property(n => n.EntityType).HasMaxLength(64);
        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead });
    }
}
