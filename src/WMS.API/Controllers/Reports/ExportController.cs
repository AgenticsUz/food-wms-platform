using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[Route("api/export")]
public class ExportController : BaseController
{
    private readonly IExportService _export;
    private readonly ITransferPdfService _transferPdf;
    public ExportController(IExportService export, ITransferPdfService transferPdf)
    { _export = export; _transferPdf = transferPdf; }

    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("transfers")]
    [RequirePermission(WmsPermissions.TransfersView)]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Transfers)]
    public async Task<IActionResult> ExportTransfers([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransfersAsync(fromDate, toDate);
        return File(bytes, ContentType, $"transfers-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("stock")]
    [RequirePermission(WmsPermissions.WarehouseView)]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.WarehouseRaw, ModuleCodes.WarehouseFinished)]
    public async Task<IActionResult> ExportStock([FromQuery] Guid? warehouseId)
    {
        var bytes = await _export.ExportStockAsync(warehouseId);
        return File(bytes, ContentType, $"stock-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("transactions")]
    [RequirePermission(WmsPermissions.FinanceView)]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Finance)]
    public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransactionsAsync(fromDate, toDate);
        return File(bytes, ContentType, $"transactions-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("products")]
    [RequirePermission(WmsPermissions.ProductsView)]
    [RequireFeature(FeatureCodes.ExportExcel)]
    public async Task<IActionResult> ExportProducts()
    {
        var bytes = await _export.ExportProductsAsync();
        return File(bytes, ContentType, $"products-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("counterparties")]
    [RequirePermission(WmsPermissions.PartnersView)]
    [RequireFeature(FeatureCodes.ExportExcel)]
    [RequireModule(ModuleCodes.Suppliers, ModuleCodes.Clients)]
    public async Task<IActionResult> ExportCounterparties([FromQuery] CounterpartyType? type)
    {
        var bytes = await _export.ExportCounterpartiesAsync(type);
        return File(bytes, ContentType, $"counterparties-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("transfers/{id:guid}/pdf")]
    [RequirePermission(WmsPermissions.TransfersView)]
    [RequireFeature(FeatureCodes.ExportPdf)]
    [RequireModule(ModuleCodes.Transfers)]
    public async Task<IActionResult> ExportTransferPdf(Guid id)
    {
        try
        {
            var bytes = await _transferPdf.GenerateTransferPdfAsync(id);
            return File(bytes, "application/pdf", $"transfer-{id}.pdf");
        }
        catch (AppException ex) when (ex is PaymentRequiredException
                                      or ModuleDisabledException
                                      or FeatureDisabledException)
        {
            // Obuna va huquq rad javoblari o'z status va kodini olib yuradi — 400 ga
            // tekislanmasin, exception middleware ularni o'zi xaritalaydi.
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
