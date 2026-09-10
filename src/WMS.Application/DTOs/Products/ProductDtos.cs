using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public List<CategoryDto> Children { get; set; } = new();
}

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}

public class UnitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}

public class CreateUnitDto
{
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
}

public class UpdateUnitDto
{
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}
