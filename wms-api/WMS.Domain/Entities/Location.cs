using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Location : BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
}
