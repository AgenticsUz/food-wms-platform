namespace WMS.Application.Interfaces;

public interface ITransferPdfService
{
    Task<byte[]> GenerateTransferPdfAsync(int transferId, int tenantId);
}
