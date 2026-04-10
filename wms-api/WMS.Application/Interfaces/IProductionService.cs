using WMS.Application.DTOs.Production;
using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IProductionService
{
    // Stages
    Task<List<ProductionStageDto>> GetStagesAsync(int tenantId);
    Task<ProductionStageDto> CreateStageAsync(int tenantId, CreateProductionStageDto dto);
    Task<ProductionStageDto> UpdateStageAsync(int tenantId, int id, UpdateProductionStageDto dto);
    Task DeleteStageAsync(int tenantId, int id);
    Task ReorderStagesAsync(int tenantId, List<ReorderStageDto> stages);

    // Recipes
    Task<List<ProductionRecipeDto>> GetRecipesAsync(int tenantId);
    Task<ProductionRecipeDto> GetRecipeByIdAsync(int tenantId, int id);
    Task<ProductionRecipeDto> CreateRecipeAsync(int tenantId, CreateRecipeDto dto);
    Task<ProductionRecipeDto> UpdateRecipeAsync(int tenantId, int id, CreateRecipeDto dto);
    Task DeleteRecipeAsync(int tenantId, int id);

    // Orders
    Task<List<ProductionOrderDto>> GetOrdersAsync(int tenantId, ProductionOrderStatus? status = null);
    Task<ProductionOrderDto> GetOrderByIdAsync(int tenantId, int id);
    Task<ProductionOrderDto> CreateOrderAsync(int tenantId, CreateProductionOrderDto dto);
    Task<ProductionOrderDto> StartOrderAsync(int tenantId, int id);
    Task<StageExecutionDto> ExecuteStageAsync(int tenantId, int orderId, int stageId, ExecuteStageDto dto);
    Task<ProductionOrderDto> CompleteOrderAsync(int tenantId, int id);
}
