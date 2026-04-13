using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Import;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[Route("api/import")]
public class ImportController : BaseController
{
    private readonly IImportService _import;
    public ImportController(IImportService import) => _import = import;

    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        using var stream = file.OpenReadStream();
        var result = await _import.ImportProductsAsync(TenantId, stream);
        return Ok(ApiResponse<ImportResultDto>.Ok(result));
    }

    [HttpPost("counterparties")]
    public async Task<IActionResult> ImportCounterparties(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        using var stream = file.OpenReadStream();
        var result = await _import.ImportCounterpartiesAsync(TenantId, stream);
        return Ok(ApiResponse<ImportResultDto>.Ok(result));
    }

    [HttpPost("users")]
    public async Task<IActionResult> ImportUsers(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        using var stream = file.OpenReadStream();
        var result = await _import.ImportUsersAsync(TenantId, stream);
        return Ok(ApiResponse<ImportResultDto>.Ok(result));
    }

    [HttpGet("template/products")]
    public IActionResult DownloadProductsTemplate()
    {
        var bytes = _import.GenerateProductsTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "products_template.xlsx");
    }

    [HttpGet("template/counterparties")]
    public IActionResult DownloadCounterpartiesTemplate()
    {
        var bytes = _import.GenerateCounterpartiesTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "counterparties_template.xlsx");
    }

    [HttpGet("template/users")]
    public IActionResult DownloadUsersTemplate()
    {
        var bytes = _import.GenerateUsersTemplate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "users_template.xlsx");
    }
}
