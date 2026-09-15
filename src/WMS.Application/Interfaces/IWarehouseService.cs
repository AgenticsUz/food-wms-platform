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

    // ── Standart ombor (P2.6) ──

    /// <summary>
    /// Tenant sozlamasi, joriy xodimning shaxsiy tanlovi va ULARDAN hisoblangan amaldagi ombor.
    /// </summary>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Sozlama va hisoblangan qiymatlar.</returns>
    Task<WarehouseDefaultsDto> GetDefaultsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Tenantning standart omborlarini saqlaydi (<c>settings.modules</c> — tenant egasining qarori).
    /// </summary>
    /// <param name="dto">Faqat <c>RawWarehouseId</c> va <c>FinishedWarehouseId</c> o'qiladi.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <remarks>
    /// Ko'rsatilgan ombor shu tenantniki va o'chirilmagan bo'lishi SHART — bu ustunlarda FK yo'q,
    /// ya'ni noto'g'ri qiymatni bazadan boshqa hech kim ushlab qolmaydi.
    /// </remarks>
    Task SetDefaultsAsync(WarehouseDefaultsDto dto, CancellationToken cancellationToken);

    /// <summary>Joriy xodimning shaxsiy standart ombori; <see langword="null"/> — tanlovni olib tashlaydi.</summary>
    /// <param name="warehouseId">Ombor yoki <see langword="null"/>.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    Task SetMyDefaultWarehouseAsync(Guid? warehouseId, CancellationToken cancellationToken);
}
