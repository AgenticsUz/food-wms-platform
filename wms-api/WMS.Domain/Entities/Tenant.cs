using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    // ── Billing poydevor hook'lari (Bosqich 5 uchun rezerv — hozir majburlanmaydi) ──
    // Plan mapping keyin shu maydon orqali TenantModule tizimiga ulanadi.
    public string? PlanType { get; set; }                                    // "trial" | "basic" | "pro" | null
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;

    // Subscription plan (control plane) — nullable; SetNull if the plan is deleted.
    public int? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}
