using WMS.Domain.Common;

namespace WMS.Domain.Entities;

/// <summary>
/// A real company, identified above the tenant boundary. A Counterparty is one tenant's
/// private record of a company; an Organization is the company itself, so the same firm
/// referenced by two tenants — or later becoming a tenant of its own — is one identity.
///
/// Matching is by INN (STIR) only. Tenants can never list or search organizations:
/// that would hand every customer a copy of the others' customer base.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = null!;

    /// Uzbek taxpayer id — 9 digits. Unique when present, nullable because most records
    /// will not have it at first.
    public string? Inn { get; set; }

    public string? Phone { get; set; }
    public string? Address { get; set; }
}
