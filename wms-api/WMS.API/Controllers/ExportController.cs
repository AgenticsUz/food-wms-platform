using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/export")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class ExportController : BaseController
{
    private readonly IExportService _export;
    public ExportController(IExportService export) => _export = export;

    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("transfers")]
    public async Task<IActionResult> ExportTransfers([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransfersAsync(TenantId, fromDate, toDate);
        return File(bytes, ContentType, $"transfers-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("stock")]
    public async Task<IActionResult> ExportStock([FromQuery] int? warehouseId)
    {
        var bytes = await _export.ExportStockAsync(TenantId, warehouseId);
        return File(bytes, ContentType, $"stock-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var bytes = await _export.ExportTransactionsAsync(TenantId, fromDate, toDate);
        return File(bytes, ContentType, $"transactions-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("products")]
    public async Task<IActionResult> ExportProducts()
    {
        var bytes = await _export.ExportProductsAsync(TenantId);
        return File(bytes, ContentType, $"products-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    [HttpGet("counterparties")]
    public async Task<IActionResult> ExportCounterparties([FromQuery] CounterpartyType? type)
    {
        var bytes = await _export.ExportCounterpartiesAsync(TenantId, type);
        return File(bytes, ContentType, $"counterparties-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }
}
