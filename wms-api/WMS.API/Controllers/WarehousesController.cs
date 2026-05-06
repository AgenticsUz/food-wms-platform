using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

public class WarehousesController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public WarehousesController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<WarehouseDto>>.Ok(await _warehouses.GetAllAsync(TenantId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        => Ok(ApiResponse<WarehouseDto>.Ok(await _warehouses.CreateAsync(TenantId, dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseDto dto)
        => Ok(ApiResponse<WarehouseDto>.Ok(await _warehouses.UpdateAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _warehouses.DeleteAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }

    [HttpGet("{id}/stock")]
    public async Task<IActionResult> GetStock(int id)
        => Ok(ApiResponse<List<StockDto>>.Ok(await _warehouses.GetStockAsync(TenantId, id)));

    [HttpGet("{id}/stock/detail")]
    public async Task<IActionResult> GetStockDetail(int id)
        => Ok(ApiResponse<List<StockDetailDto>>.Ok(await _warehouses.GetStockDetailAsync(TenantId, id)));
}

[ApiController]
[Route("api/locations")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class LocationsController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public LocationsController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? warehouseId)
        => Ok(ApiResponse<List<LocationDto>>.Ok(await _warehouses.GetLocationsAsync(TenantId, warehouseId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationDto dto)
        => Ok(ApiResponse<LocationDto>.Ok(await _warehouses.CreateLocationAsync(TenantId, dto)));
}

[ApiController]
[Route("api/batches")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class BatchesController : BaseController
{
    private readonly IWarehouseService _warehouses;
    public BatchesController(IWarehouseService warehouses) => _warehouses = warehouses;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<BatchDto>>.Ok(await _warehouses.GetBatchesAsync(TenantId)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBatchDto dto)
        => Ok(ApiResponse<BatchDto>.Ok(await _warehouses.UpdateBatchAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _warehouses.DeleteBatchAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}
