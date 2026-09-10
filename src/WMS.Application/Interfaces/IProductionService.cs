using WMS.Application.DTOs.Production;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IProductionService
{
    // Stages
    Task<List<ProductionStageDto>> GetStagesAsync();
    Task<ProductionStageDto> CreateStageAsync(CreateProductionStageDto dto);
    Task<ProductionStageDto> UpdateStageAsync(Guid id, UpdateProductionStageDto dto);
    Task DeleteStageAsync(Guid id);
    Task ReorderStagesAsync(List<Guid> ids);

    // Recipes
    Task<List<ProductionRecipeDto>> GetRecipesAsync();
    Task<ProductionRecipeDto> GetRecipeByIdAsync(Guid id);
    Task<ProductionRecipeDto> CreateRecipeAsync(CreateRecipeDto dto);
    Task<ProductionRecipeDto> UpdateRecipeAsync(Guid id, CreateRecipeDto dto);
    Task DeleteRecipeAsync(Guid id);

    // Orders
    Task<List<ProductionOrderDto>> GetOrdersAsync(ProductionOrderStatus? status = null);
    Task<ProductionOrderDto> GetOrderByIdAsync(Guid id);
    Task<ProductionOrderDto> CreateOrderAsync(CreateProductionOrderDto dto);
    Task<ProductionOrderDto> StartOrderAsync(Guid id);
    Task<StageExecutionDto> ExecuteStageAsync(Guid orderId, Guid stageId, ExecuteStageDto dto);
    Task<ProductionOrderDto> CompleteOrderAsync(Guid id);
}
