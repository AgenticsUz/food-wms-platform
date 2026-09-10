using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.DTOs.Import;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

// F6: `POST users` va `GET template/users` O'CHDI (D7) — foydalanuvchi Identity'da Console
// orqali yaratiladi; WMS'da parol ham, a'zolik ham yo'q.
[Route("api/import")]
[RequireFeature(FeatureCodes.ImportExcel)]
public class ImportController : BaseController
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IImportService _import;
    public ImportController(IImportService import) => _import = import;

    [HttpPost("products")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public async Task<IActionResult> ImportProducts(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        using var stream = file.OpenReadStream();
        var result = await _import.ImportProductsAsync(stream);
        return Ok(ApiResponse<ImportResultDto>.Ok(result));
    }

    [HttpPost("counterparties")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    [RequireModule(ModuleCodes.Suppliers, ModuleCodes.Clients)]
    public async Task<IActionResult> ImportCounterparties(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        using var stream = file.OpenReadStream();
        var result = await _import.ImportCounterpartiesAsync(stream);
        return Ok(ApiResponse<ImportResultDto>.Ok(result));
    }

    [HttpGet("template/products")]
    [RequirePermission(WmsPermissions.ProductsManage)]
    public IActionResult DownloadProductsTemplate()
        => File(_import.GenerateProductsTemplate(), ContentType, "products_template.xlsx");

    [HttpGet("template/counterparties")]
    [RequirePermission(WmsPermissions.PartnersManage)]
    public IActionResult DownloadCounterpartiesTemplate()
        => File(_import.GenerateCounterpartiesTemplate(), ContentType, "counterparties_template.xlsx");
}
