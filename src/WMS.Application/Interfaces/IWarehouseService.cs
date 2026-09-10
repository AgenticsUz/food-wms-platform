using WMS.Application.DTOs.Warehouses;

namespace WMS.Application.Interfaces;

public interface IWarehouseService
{
    Task<List<WarehouseDto>> GetAllAsync();
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto);
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto);
    Task DeleteAsync(Guid id);
    Task<List<StockDto>> GetStockAsync(Guid warehouseId);
    Task<List<StockDetailDto>> GetStockDetailAsync(Guid warehouseId);
    Task<List<LocationDto>> GetLocationsAsync(Guid? warehouseId = null);
    Task<LocationDto> CreateLocationAsync(CreateLocationDto dto);
    Task<LocationDto> UpdateLocationAsync(Guid id, CreateLocationDto dto);
    Task DeleteLocationAsync(Guid id);
    Task<List<BatchDto>> GetBatchesAsync();
    Task<BatchDto> UpdateBatchAsync(Guid id, UpdateBatchDto dto);
    Task DeleteBatchAsync(Guid id);
}
