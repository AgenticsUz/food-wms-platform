using WMS.Application.DTOs.Products;

namespace WMS.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(int tenantId, int page = 1, int pageSize = 20);
    Task<ProductDto> CreateAsync(int tenantId, CreateProductDto dto);
    Task<ProductDto> UpdateAsync(int tenantId, int id, UpdateProductDto dto);
    Task DeleteAsync(int tenantId, int id);
    Task<List<CategoryDto>> GetCategoriesAsync(int tenantId);
    Task<CategoryDto> CreateCategoryAsync(int tenantId, CreateCategoryDto dto);
    Task<CategoryDto> UpdateCategoryAsync(int tenantId, int id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(int tenantId, int id);
    Task<List<UnitDto>> GetUnitsAsync(int tenantId);
    Task<UnitDto> CreateUnitAsync(int tenantId, CreateUnitDto dto);
    Task<UnitDto> UpdateUnitAsync(int tenantId, int id, UpdateUnitDto dto);
    Task DeleteUnitAsync(int tenantId, int id);
}
