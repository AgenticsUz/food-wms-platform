namespace WMS.Application.Interfaces;

public interface IBatchExpiryService
{
    /// <summary>
    /// JORIY tenantning partiyalarini tekshiradi (tenant — so'rov yoki fon scope'ining konteksti, D4/D12).
    /// </summary>
    Task<int> CheckBatchesAsync(int warningDaysAhead = 3);
}
