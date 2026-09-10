using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Warehouses;

public class WarehouseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public WarehouseType Type { get; set; }
    public string? Description { get; set; }
}

public class CreateWarehouseDto
{
    public string Name { get; set; } = null!;
    public WarehouseType Type { get; set; }
    public string? Description { get; set; }
}

public class UpdateWarehouseDto
{
    public string Name { get; set; } = null!;
    public WarehouseType Type { get; set; }
    public string? Description { get; set; }
}

public class LocationDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
}

public class CreateLocationDto
{
    public Guid WarehouseId { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
}

public class StockDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public decimal TotalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
}

public class StockDetailDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public Guid BatchId { get; set; }
    public string LotNumber { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
}

public class BatchDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string LotNumber { get; set; } = null!;
    public DateTime ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBatchDto
{
    public string LotNumber { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}
