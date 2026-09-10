using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Production;

public class ProductionStageDto
{
    public Guid Id { get; set; }
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
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
}

public class ReorderStagesDto
{
    public List<Guid> Ids { get; set; } = new();
}

public class ProductionRecipeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid OutputProductId { get; set; }
    public string OutputProductName { get; set; } = null!;
    public decimal OutputQuantity { get; set; }
    public Guid OutputUnitId { get; set; }
    public string OutputUnitName { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<RecipeStageDto> Stages { get; set; } = new();
}

public class RecipeStageDto
{
    public Guid Id { get; set; }
    public Guid StageId { get; set; }
    public string StageName { get; set; } = null!;
    public int OrderNumber { get; set; }
    public Guid? OutputProductId { get; set; }
    public string? OutputProductName { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; }
    public Guid? OutputWarehouseId { get; set; }
    public List<RecipeStageItemDto> Inputs { get; set; } = new();
}

public class RecipeStageItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = null!;
}

public class CreateRecipeDto
{
    public string Name { get; set; } = null!;
    public Guid OutputProductId { get; set; }
    public decimal OutputQuantity { get; set; }
    public Guid OutputUnitId { get; set; }
    public List<CreateRecipeStageDto> Stages { get; set; } = new();
}

public class CreateRecipeStageDto
{
    public Guid StageId { get; set; }
    public int OrderNumber { get; set; }
    public Guid? OutputProductId { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; }
    public Guid? OutputWarehouseId { get; set; }
    public List<CreateRecipeStageItemDto> Inputs { get; set; } = new();
}

public class CreateRecipeStageItemDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
}

public class ProductionOrderDto
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public string RecipeName { get; set; } = null!;
    public string OutputProductName { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public ProductionOrderStatus Status { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StageExecutionDto> StageExecutions { get; set; } = new();
}

public class CreateProductionOrderDto
{
    public Guid RecipeId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Note { get; set; }
}

public class StageExecutionDto
{
    public Guid Id { get; set; }
    public Guid RecipeStageId { get; set; }
    public string StageName { get; set; } = null!;
    public int OrderNumber { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public decimal ReworkQuantity { get; set; }
    public Guid? WorkerUserId { get; set; }
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
    public Guid? WorkerUserId { get; set; }
    public string? Note { get; set; }
}
