using WMS.Application.DTOs.Warehouses;

namespace WMS.Application.Interfaces;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllAsync(int tenantId);
    Task<WarehouseDto> CreateAsync(int tenantId, CreateWarehouseDto dto);
    Task<WarehouseDto> UpdateAsync(int tenantId, int id, UpdateWarehouseDto dto);
    Task DeleteAsync(int tenantId, int id);
    Task<List<StockDto>> GetStockAsync(int tenantId, int warehouseId);
    Task<List<StockDetailDto>> GetStockDetailAsync(int tenantId, int warehouseId);
    Task<List<LocationDto>> GetLocationsAsync(int tenantId, int? warehouseId = null);
    Task<LocationDto> CreateLocationAsync(int tenantId, CreateLocationDto dto);
    Task<List<BatchDto>> GetBatchesAsync(int tenantId);
    Task<BatchDto> UpdateBatchAsync(int tenantId, int id, UpdateBatchDto dto);
    Task DeleteBatchAsync(int tenantId, int id);
}
