using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// One manually recorded subscription payment: "tenant X paid for this period".
/// Platform-level — TenantId is the tenant the payment is FOR, and only SuperAdmin
/// reads or writes these rows. This is the seed the real billing module grows from.
/// </summary>
public class PaymentRecord : BaseEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UZS";
    public PlatformPaymentMethod Method { get; set; } = PlatformPaymentMethod.BankTransfer;
    public string? Note { get; set; }

    public int? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
