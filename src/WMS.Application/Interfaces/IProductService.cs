using WMS.Application.DTOs.Products;

namespace WMS.Application.Interfaces;

public interface IProductService
{
    /// <summary>
    /// Mahsulotlar ro'yxati; <paramref name="search"/> berilsa — nom bo'yicha taxminiy
    /// qidiruv (P2.1, <c>ISearchService</c>), tartib o'xshashlik bali bo'yicha.
    /// </summary>
    /// <param name="page">Sahifa (1 dan).</param>
    /// <param name="pageSize">Sahifa hajmi.</param>
    /// <param name="search">Qidiruv matni; bo'sh bo'lsa — oddiy ro'yxat (eski xatti-harakat).</param>
    /// <returns>Mahsulotlar.</returns>
    Task<List<ProductDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null);
    Task<ProductDto> GetByIdAsync(Guid id);

    /// <summary>
    /// Shtrix-kod bo'yicha mahsulot (skaner klaviatura sifatida kodni `Enter` bilan yuboradi, D1).
    /// </summary>
    Task<ProductDto> GetByBarcodeAsync(string barcode);

    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto);
    Task DeleteAsync(Guid id);
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);
    Task<List<UnitDto>> GetUnitsAsync();
    Task<UnitDto> CreateUnitAsync(CreateUnitDto dto);
    Task<UnitDto> UpdateUnitAsync(Guid id, UpdateUnitDto dto);
    Task DeleteUnitAsync(Guid id);
}
