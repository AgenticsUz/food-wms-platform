using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Vehicle : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;   // plate / label
    public string? Model { get; set; }
    public decimal Capacity { get; set; }        // kg or units
    public bool IsActive { get; set; } = true;
}
