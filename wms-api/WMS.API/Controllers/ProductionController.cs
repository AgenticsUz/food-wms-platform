using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Production;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/production")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission("production.view")]
[RequireModule(ModuleCodes.Production)]
public class ProductionController : BaseController
{
    private readonly IProductionService _production;
    public ProductionController(IProductionService production) => _production = production;

    // Stages
    [HttpGet("stages")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    public async Task<IActionResult> GetStages()
        => Ok(ApiResponse<List<ProductionStageDto>>.Ok(await _production.GetStagesAsync(TenantId)));

    [HttpPost("stages")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> CreateStage([FromBody] CreateProductionStageDto dto)
        => Ok(ApiResponse<ProductionStageDto>.Ok(await _production.CreateStageAsync(TenantId, dto)));

    [HttpPut("stages/{id}")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> UpdateStage(int id, [FromBody] UpdateProductionStageDto dto)
        => Ok(ApiResponse<ProductionStageDto>.Ok(await _production.UpdateStageAsync(TenantId, id, dto)));

    [HttpDelete("stages/{id}")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> DeleteStage(int id)
    { await _production.DeleteStageAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpPatch("stages/reorder")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> ReorderStages([FromBody] ReorderStagesDto dto)
    {
        await _production.ReorderStagesAsync(TenantId, dto.Ids);
        return Ok(ApiResponse<object>.Ok(null!, "Reordered"));
    }

    // Recipes
    [HttpGet("recipes")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    public async Task<IActionResult> GetRecipes()
        => Ok(ApiResponse<List<ProductionRecipeDto>>.Ok(await _production.GetRecipesAsync(TenantId)));

    [HttpGet("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    public async Task<IActionResult> GetRecipe(int id)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.GetRecipeByIdAsync(TenantId, id)));

    [HttpPost("recipes")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeDto dto)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.CreateRecipeAsync(TenantId, dto)));

    [HttpPut("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> UpdateRecipe(int id, [FromBody] CreateRecipeDto dto)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.UpdateRecipeAsync(TenantId, id, dto)));

    [HttpDelete("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> DeleteRecipe(int id)
    { await _production.DeleteRecipeAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // Orders
    [HttpGet("orders")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    public async Task<IActionResult> GetOrders([FromQuery] ProductionOrderStatus? status)
        => Ok(ApiResponse<List<ProductionOrderDto>>.Ok(await _production.GetOrdersAsync(TenantId, status)));

    [HttpGet("orders/{id}")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    public async Task<IActionResult> GetOrder(int id)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.GetOrderByIdAsync(TenantId, id)));

    [HttpPost("orders")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateProductionOrderDto dto)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.CreateOrderAsync(TenantId, dto)));

    [HttpPut("orders/{id}/start")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> StartOrder(int id)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.StartOrderAsync(TenantId, id)));

    [HttpPut("orders/{id}/stages/{stageId}/execute")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> ExecuteStage(int id, int stageId, [FromBody] ExecuteStageDto dto)
        => Ok(ApiResponse<StageExecutionDto>.Ok(await _production.ExecuteStageAsync(TenantId, id, stageId, dto)));

    [HttpPut("orders/{id}/complete")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission("production.manage")]
    public async Task<IActionResult> CompleteOrder(int id)
    {
        try
        {
            var result = await _production.CompleteOrderAsync(TenantId, id);
            return Ok(ApiResponse<ProductionOrderDto>.Ok(result));
        }
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Entitlement and subscription refusals carry their own status and code —
            // let the exception middleware map them instead of flattening to 400.
            throw;
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
