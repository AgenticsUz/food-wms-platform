using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

public class ProductsController : BaseController
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(ApiResponse<List<ProductDto>>.Ok(await _products.GetAllAsync(TenantId, page, pageSize)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.CreateAsync(TenantId, dto)));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.UpdateAsync(TenantId, id, dto)));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    { await _products.DeleteAsync(TenantId, id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[ApiController]
[Route("api/categories")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class CategoriesController : BaseController
{
    private readonly IProductService _products;
    public CategoriesController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<CategoryDto>>.Ok(await _products.GetCategoriesAsync(TenantId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        => Ok(ApiResponse<CategoryDto>.Ok(await _products.CreateCategoryAsync(TenantId, dto)));
}

[ApiController]
[Route("api/units")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class UnitsController : BaseController
{
    private readonly IProductService _products;
    public UnitsController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<UnitDto>>.Ok(await _products.GetUnitsAsync(TenantId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
        => Ok(ApiResponse<UnitDto>.Ok(await _products.CreateUnitAsync(TenantId, dto)));
}
