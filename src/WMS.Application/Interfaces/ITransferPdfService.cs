namespace WMS.Application.Interfaces;

public interface ITransferPdfService
{
    /// <summary>Joriy tenantdagi transfer hujjati (A4, tenant brendi bilan).</summary>
    Task<byte[]> GenerateTransferPdfAsync(Guid transferId, CancellationToken ct = default);
}
