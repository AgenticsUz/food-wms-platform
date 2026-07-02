namespace WMS.Application.Interfaces;

/// <summary>
/// KELAJAK STUB (Bosqich 5B — AI Advisor). Hozir IMPLEMENTATSIYA YO'Q va DI'ga
/// ro'yxatdan o'tkazilmagan — faqat integratsiya joyini (seam) belgilaydi.
/// Keyin Claude API ustiga quriladi va mavjud toza analitika endpoint'lari + tarixiy
/// data'dan (sotuv, ishlab chiqarish, chiqindi, batch muddati) o'qiydi.
/// </summary>
public interface IAiAdvisorService
{
    // Sotuv tarixiga qarab zaxira bashorati (restock tavsiyasi).
    Task<string> GetStockForecastAsync(int tenantId, int productId, int horizonDays);

    // Chiqindi kamaytirish / ishlab chiqarish rejasi tavsiyasi.
    Task<string> GetProductionAdviceAsync(int tenantId, int days);
}
