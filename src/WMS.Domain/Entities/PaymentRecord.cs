using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// Qo'lda qayd etilgan obuna to'lovi: «tenant X shu davr uchun to'ladi».
/// Faqat Console'ning WMS bo'limi yozadi va o'qiydi.
/// </summary>
/// <remarks>
/// RLS ostida: Console tenantni OSHKORA tanlaydi (<c>X-Tenant-Id</c>, §4.4), ya'ni
/// yozuv o'sha tenant kontekstida yoziladi. Tenantlar bo'yicha umumiy ko'rinish
/// (muddati tugayotganlar) <c>tenant.paid_until</c> dan olinadi — u platforma jadvali.
/// </remarks>
public class PaymentRecord : TenantEntity
{
    public Tenant Tenant { get; set; } = null!;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    public PlatformPaymentMethod Method { get; set; } = PlatformPaymentMethod.BankTransfer;
    public string? Note { get; set; }

    /// <summary>Qayd etgan Console operatorining Identity <c>sub</c>'i.</summary>
    public Guid? RecordedBySub { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
