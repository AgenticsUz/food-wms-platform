namespace WMS.Application.Interfaces;

public interface IDeliveryPdfService
{
    Task<byte[]> GenerateWaybillPdfAsync(int deliveryId, int tenantId);
}
