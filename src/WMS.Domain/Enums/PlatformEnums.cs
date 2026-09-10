namespace WMS.Domain.Enums;

/// <summary>
/// Tenant obuna davri uchun qanday to'lagani (qo'lda billing). Tenantning O'Z
/// mijozlari to'lovi (<see cref="PaymentMethod"/>) bilan aloqasi yo'q.
/// </summary>
public enum PlatformPaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 4
}

/// <summary>
/// Nega to'xtatilgan. Mijoz oladigan rad kodi shundan quriladi — «to'lamadingiz»,
/// «o'zingiz so'radingiz» va «texnik ish» bir-biridan ajralib tursin.
/// </summary>
public enum SuspendReason
{
    NonPayment = 1,
    ClientRequest = 2,
    Technical = 3,
    Violation = 4,
    Other = 5
}
