using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class AttendanceLog : BaseEntity
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public AttendanceMethod Method { get; set; }
    public string? DeviceId { get; set; }
}
