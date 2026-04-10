using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Shift : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
