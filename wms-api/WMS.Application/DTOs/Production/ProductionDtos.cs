using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Production;

public class ProductionStageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}

public class CreateProductionStageDto
{
    public string Name { get; set; } = null!;
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}

public class UpdateProductionStageDto
{
    public string Name { get; set; } = null!;
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}

public class ReorderStageDto
{
    public int Id { get; set; }
    public int OrderNumber { get; set; }
}

public class ProductionRecipeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int OutputProductId { get; set; }
    public string OutputProductName { get; set; } = null!;
    public decimal OutputQuantity { get; set; }
    public int OutputUnitId { get; set; }
    public string OutputUnitName { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<RecipeStageDto> Stages { get; set; } = new();
}

public class RecipeStageDto
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public string StageName { get; set; } = null!;
    public int OrderNumber { get; set; }
    public int? OutputProductId { get; set; }
    public string? OutputProductName { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; }
    public int? OutputWarehouseId { get; set; }
    public List<RecipeStageItemDto> Inputs { get; set; } = new();
}

public class RecipeStageItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = null!;
}

public class CreateRecipeDto
{
    public string Name { get; set; } = null!;
    public int OutputProductId { get; set; }
    public decimal OutputQuantity { get; set; }
    public int OutputUnitId { get; set; }
    public List<CreateRecipeStageDto> Stages { get; set; } = new();
}

public class CreateRecipeStageDto
{
    public int StageId { get; set; }
    public int OrderNumber { get; set; }
    public int? OutputProductId { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; }
    public int? OutputWarehouseId { get; set; }
    public List<CreateRecipeStageItemDto> Inputs { get; set; } = new();
}

public class CreateRecipeStageItemDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
}

public class ProductionOrderDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = null!;
    public string OutputProductName { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public ProductionOrderStatus Status { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StageExecutionDto> StageExecutions { get; set; } = new();
}

public class CreateProductionOrderDto
{
    public int RecipeId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Note { get; set; }
}

public class StageExecutionDto
{
    public int Id { get; set; }
    public int RecipeStageId { get; set; }
    public string StageName { get; set; } = null!;
    public int OrderNumber { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public decimal ReworkQuantity { get; set; }
    public int? WorkerUserId { get; set; }
    public string? WorkerUserName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public StageExecutionStatus Status { get; set; }
    public string? Note { get; set; }
}

public class ExecuteStageDto
{
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public decimal ReworkQuantity { get; set; }
    public int? WorkerUserId { get; set; }
    public string? Note { get; set; }
}
