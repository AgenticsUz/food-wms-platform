using WMS.Application.DTOs.Products;

namespace WMS.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(int page = 1, int pageSize = 20);
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
