using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[RequirePermission(WmsPermissions.WarehouseView)]
[RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
public class WarehousesController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public WarehousesController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<WarehouseDto>>.Ok(await _warehouses.GetAllAsync()));

    [HttpPost]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        => Ok(ApiResponse<WarehouseDto>.Ok(await _warehouses.CreateAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseDto dto)
        => Ok(ApiResponse<WarehouseDto>.Ok(await _warehouses.UpdateAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _warehouses.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id:guid}/stock")]
    [RequireFeature(FeatureCodes.WarehouseStock)]
    public async Task<IActionResult> GetStock(Guid id)
        => Ok(ApiResponse<List<StockDto>>.Ok(await _warehouses.GetStockAsync(id)));

    [HttpGet("{id:guid}/stock/detail")]
    [RequireFeature(FeatureCodes.WarehouseStock)]
    public async Task<IActionResult> GetStockDetail(Guid id)
        => Ok(ApiResponse<List<StockDetailDto>>.Ok(await _warehouses.GetStockDetailAsync(id)));
}

[ApiController]
[Route("api/locations")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission(WmsPermissions.WarehouseView)]
[RequireFeature(FeatureCodes.WarehouseLocations)]
public class LocationsController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public LocationsController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? warehouseId)
        => Ok(ApiResponse<List<LocationDto>>.Ok(await _warehouses.GetLocationsAsync(warehouseId)));

    [HttpPost]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Create([FromBody] CreateLocationDto dto)
        => Ok(ApiResponse<LocationDto>.Ok(await _warehouses.CreateLocationAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLocationDto dto)
        => Ok(ApiResponse<LocationDto>.Ok(await _warehouses.UpdateLocationAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _warehouses.DeleteLocationAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[ApiController]
[Route("api/batches")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission(WmsPermissions.WarehouseView)]
[RequireFeature(FeatureCodes.WarehouseBatches)]
public class BatchesController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public BatchesController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<BatchDto>>.Ok(await _warehouses.GetBatchesAsync()));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBatchDto dto)
        => Ok(ApiResponse<BatchDto>.Ok(await _warehouses.UpdateBatchAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.WarehouseManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _warehouses.DeleteBatchAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}
