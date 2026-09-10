using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[RequirePermission(WmsPermissions.ProductsView)]
public class ProductsController : BaseController
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(ApiResponse<List<ProductDto>>.Ok(await _products.GetAllAsync(page, pageSize)));

    /// Skaner (klaviatura) kodni `Enter` bilan yuboradi (D1). Kod so'rov satrida, yo'lda emas:
    /// shtrix-kodda `/` kabi belgilar bo'lishi mumkin. `{id:guid}` dan OLDIN e'lon qilingani
    /// shart emas — cheklov ikkisini ajratadi.
    [HttpGet("by-barcode")]
    public async Task<IActionResult> GetByBarcode([FromQuery] string code)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.GetByBarcodeAsync(code)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.GetByIdAsync(id)));

    [HttpPost]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.CreateAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        => Ok(ApiResponse<ProductDto>.Ok(await _products.UpdateAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _products.DeleteAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[ApiController]
[Route("api/categories")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission(WmsPermissions.ProductsView)]
public class CategoriesController : BaseController
{
    private readonly IProductService _products;
    public CategoriesController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<CategoryDto>>.Ok(await _products.GetCategoriesAsync()));

    [HttpPost]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        => Ok(ApiResponse<CategoryDto>.Ok(await _products.CreateCategoryAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
        => Ok(ApiResponse<CategoryDto>.Ok(await _products.UpdateCategoryAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _products.DeleteCategoryAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}

[ApiController]
[Route("api/units")]
[Microsoft.AspNetCore.Authorization.Authorize]
[RequirePermission(WmsPermissions.ProductsView)]
public class UnitsController : BaseController
{
    private readonly IProductService _products;
    public UnitsController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<UnitDto>>.Ok(await _products.GetUnitsAsync()));

    [HttpPost]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
        => Ok(ApiResponse<UnitDto>.Ok(await _products.CreateUnitAsync(dto)));

    [HttpPut("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitDto dto)
        => Ok(ApiResponse<UnitDto>.Ok(await _products.UpdateUnitAsync(id, dto)));

    [HttpDelete("{id:guid}")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> Delete(Guid id)
    { await _products.DeleteUnitAsync(id); return Ok(ApiResponse<object>.Ok(null!, "Deleted")); }
}
