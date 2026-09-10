namespace WMS.Application.Interfaces;

public interface IDeliveryPdfService
{
    Task<byte[]> GenerateWaybillPdfAsync(Guid deliveryId, CancellationToken ct = default);
}
