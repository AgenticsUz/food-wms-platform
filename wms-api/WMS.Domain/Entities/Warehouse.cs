using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Warehouse : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public WarehouseType Type { get; set; }
    public string? Description { get; set; }
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}
