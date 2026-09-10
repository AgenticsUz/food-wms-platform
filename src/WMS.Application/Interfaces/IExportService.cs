using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

/// <remarks>F6: <c>tenantId</c> parametrlari o'chdi (D4) — tenant global filtr va RLS'dan.</remarks>
public interface IExportService
{
    Task<byte[]> ExportTransfersAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportStockAsync(Guid? warehouseId = null);
    Task<byte[]> ExportTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportProductsAsync();
    Task<byte[]> ExportCounterpartiesAsync(CounterpartyType? type = null);
}
