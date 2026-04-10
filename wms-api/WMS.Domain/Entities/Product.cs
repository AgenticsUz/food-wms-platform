using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Product : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; } = 0;
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}
