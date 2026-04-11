using WMS.Domain.Enums;

namespace WMS.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportTransfersAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportStockAsync(int tenantId, int? warehouseId = null);
    Task<byte[]> ExportTransactionsAsync(int tenantId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<byte[]> ExportProductsAsync(int tenantId);
    Task<byte[]> ExportCounterpartiesAsync(int tenantId, CounterpartyType? type = null);
}
