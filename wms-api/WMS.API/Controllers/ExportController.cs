using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/export")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class ExportController : BaseController
{
    private readonly IExportService _export;
    private readonly ITransferPdfService _transferPdf;
    public ExportController(IExportService export, ITransferPdfService transferPdf)
    { _export = export; _transferPdf = transferPdf; }

    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("transfers")]
    [RequirePermission("transfers.view")]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Transfers)]
    public async Task<IActionResult> ExportTransfers([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransfersAsync(TenantId, fromDate, toDate);
        return File(bytes, ContentType, $"transfers-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("stock")]
    [RequirePermission("warehouse.view")]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
    public async Task<IActionResult> ExportStock([FromQuery] int? warehouseId)
    {
        var bytes = await _export.ExportStockAsync(TenantId, warehouseId);
        return File(bytes, ContentType, $"stock-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("transactions")]
    [RequirePermission("finance.view")]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Finance)]
    public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransactionsAsync(TenantId, fromDate, toDate);
        return File(bytes, ContentType, $"transactions-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("products")]
    [RequirePermission("products.view")]
    [RequireFeature(FeatureCodes.ExportExcel)]
    public async Task<IActionResult> ExportProducts()
    {
        var bytes = await _export.ExportProductsAsync(TenantId);
        return File(bytes, ContentType, $"products-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("counterparties")]
    [RequirePermission("partners.view")]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Suppliers, ModuleCodes.Clients)]
    public async Task<IActionResult> ExportCounterparties([FromQuery] CounterpartyType? type)
    {
        var bytes = await _export.ExportCounterpartiesAsync(TenantId, type);
        return File(bytes, ContentType, $"counterparties-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("transfers/{id}/pdf")]
    [RequirePermission("transfers.view")]
    [RequireFeature(FeatureCodes.ExportPdf)]
    [RequireModule(ModuleCodes.Transfers)]
    public async Task<IActionResult> ExportTransferPdf(int id)
    {
        try
        {
            var bytes = await _transferPdf.GenerateTransferPdfAsync(id, TenantId);
            return File(bytes, "application/pdf", $"transfer-{id}.pdf");
        }
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Entitlement and subscription refusals carry their own status and code —
            // let the exception middleware map them instead of flattening to 400.
            throw;
        }
        catch (Exception ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
