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

    /// Manual billing: paid through this date. Null = never blocked for payment,
    /// which is what keeps pre-billing tenants and the system tenant working.
    public DateTime? PaidUntil { get; set; }

    // ── Suspension detail (why, until when, who) ──
    public SuspendReason? SuspendReason { get; set; }
    /// Internal note — never returned to the tenant.
    public string? SuspendNote { get; set; }
    /// Optional message shown to the tenant instead of the generic text.
    public string? SuspendPublicMessage { get; set; }
    /// Auto-reactivation date. Null = suspended until someone activates it by hand.
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public int? SuspendedByUserId { get; set; }

    // ── Branding (B1). Data, never code: one build serves every customer. ──
    /// Wide logo — expanded sidebar, login page, report headers.
    public string? LogoUrl { get; set; }
    /// Square logo — collapsed sidebar, favicon.
    public string? LogoSquareUrl { get; set; }
    /// Single primary colour (#RRGGBB); the client derives the palette from it.
    public string? BrandColor { get; set; }

    /// The real company behind this tenant (platform-level identity, see Organization).
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}
