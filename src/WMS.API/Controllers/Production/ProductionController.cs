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
[RequirePermission(WmsPermissions.ProductionView)]
[RequireModule(ModuleCodes.Production)]
public class ProductionController : BaseController
{
    private readonly IProductionService _production;
    public ProductionController(IProductionService production) => _production = production;

    // Stages
    [HttpGet("stages")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    public async Task<IActionResult> GetStages()
        => Ok(ApiResponse<List<ProductionStageDto>>.Ok(await _production.GetStagesAsync()));

    [HttpPost("stages")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> CreateStage([FromBody] CreateProductionStageDto dto)
        => Ok(ApiResponse<ProductionStageDto>.Ok(await _production.CreateStageAsync(dto)));

    [HttpPut("stages/{id}")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateProductionStageDto dto)
        => Ok(ApiResponse<ProductionStageDto>.Ok(await _production.UpdateStageAsync(id, dto)));

    [HttpDelete("stages/{id}")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> DeleteStage(Guid id)
    { await _production.DeleteStageAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpPatch("stages/reorder")]
    [RequireFeature(FeatureCodes.ProductionStages)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> ReorderStages([FromBody] ReorderStagesDto dto)
    {
        await _production.ReorderStagesAsync(dto.Ids);
        return Ok(ApiResponse<object>.Ok(null!, "Reordered"));
    }

    // Recipes
    [HttpGet("recipes")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    public async Task<IActionResult> GetRecipes()
        => Ok(ApiResponse<List<ProductionRecipeDto>>.Ok(await _production.GetRecipesAsync()));

    [HttpGet("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    public async Task<IActionResult> GetRecipe(Guid id)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.GetRecipeByIdAsync(id)));

    [HttpPost("recipes")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeDto dto)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.CreateRecipeAsync(dto)));

    [HttpPut("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> UpdateRecipe(Guid id, [FromBody] CreateRecipeDto dto)
        => Ok(ApiResponse<ProductionRecipeDto>.Ok(await _production.UpdateRecipeAsync(id, dto)));

    [HttpDelete("recipes/{id}")]
    [RequireFeature(FeatureCodes.ProductionRecipes)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    { await _production.DeleteRecipeAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    // Orders
    [HttpGet("orders")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    public async Task<IActionResult> GetOrders([FromQuery] ProductionOrderStatus? status)
        => Ok(ApiResponse<List<ProductionOrderDto>>.Ok(await _production.GetOrdersAsync(status)));

    [HttpGet("orders/{id}")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    public async Task<IActionResult> GetOrder(Guid id)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.GetOrderByIdAsync(id)));

    [HttpPost("orders")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateProductionOrderDto dto)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.CreateOrderAsync(dto)));

    [HttpPut("orders/{id}/start")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> StartOrder(Guid id)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.StartOrderAsync(id)));

    [HttpPut("orders/{id}/stages/{stageId}/execute")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> ExecuteStage(Guid id, Guid stageId, [FromBody] ExecuteStageDto dto)
        => Ok(ApiResponse<StageExecutionDto>.Ok(await _production.ExecuteStageAsync(id, stageId, dto)));

    // F6: SQLite davridagi «har xatoni 400 ga aylantir» try/catch'i olib tashlandi. U
    // DbUpdateConcurrencyException'ni ham 400 qilib yutardi — parallel ikki yakunlashda
    // ikkinchisi 409 (D13) olishi kerak. Endi hamma xatoni ExceptionHandlingMiddleware xaritalaydi:
    // AppException → 400 (tarjima bilan), NotFound → 404, poyga → 409.
    [HttpPut("orders/{id}/complete")]
    [RequireFeature(FeatureCodes.ProductionOrders)]
    [RequirePermission(WmsPermissions.ProductionManage)]
    public async Task<IActionResult> CompleteOrder(Guid id)
        => Ok(ApiResponse<ProductionOrderDto>.Ok(await _production.CompleteOrderAsync(id)));
}
