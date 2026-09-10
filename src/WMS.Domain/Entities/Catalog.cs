using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Category : TenantEntity
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class Unit : TenantEntity
{
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}

public class Product : TenantEntity
{
    public string Name { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}
