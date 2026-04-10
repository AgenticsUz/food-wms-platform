using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class WarehouseStock : BaseEntity
{
    public int TenantId { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; } = 0;
}
