using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Shift : TenantEntity
{
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class ShiftPlan : TenantEntity
{
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public DateTime Date { get; set; }
}

public class ShiftActual : TenantEntity
{
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}

public class AttendanceLog : TenantEntity
{
    public Guid UserId { get; set; }
    public UserProfile User { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public AttendanceMethod Method { get; set; }
    public string? DeviceId { get; set; }
}

public class Notification : TenantEntity
{
    /// <summary>Qabul qiluvchi profil; <see langword="null"/> — tenantdagi hammaga.</summary>
    public Guid? UserId { get; set; }
    public UserProfile? User { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
