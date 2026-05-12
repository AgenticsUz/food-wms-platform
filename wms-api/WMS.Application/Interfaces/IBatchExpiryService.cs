namespace WMS.Application.Interfaces;

public interface IBatchExpiryService
{
    Task<int> CheckBatchesAsync(int tenantId, int warningDaysAhead = 3);
}
