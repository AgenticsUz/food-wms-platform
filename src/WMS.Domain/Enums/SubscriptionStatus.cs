namespace WMS.Domain.Enums;

/// Tenant obuna holati. HOZIR hech qaerda majburlanmaydi — billing (Bosqich 5)
/// uchun rezerv qilingan poydevor hook. Default = Active.
public enum SubscriptionStatus
{
    Trial = 1,
    Active = 2,
    Suspended = 3
}
