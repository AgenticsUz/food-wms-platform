using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence;

/// <summary>
/// The single place a company is matched to a platform-level <see cref="Organization"/>.
/// Matching is by INN only: names are typed differently in every tenant ("Ice Gold",
/// "ICE GOLD MChJ", "ice-gold"), so name matching would merge unrelated firms.
///
/// No INN means no organization — the counterparty simply stays local to its tenant until
/// someone fills the number in.
/// </summary>
public static class OrganizationMatcher
{
    private static readonly Regex InnPattern = new(@"^\d{9}$", RegexOptions.Compiled);

    /// Normalizes and validates an Uzbek taxpayer id (STIR): 9 digits, spaces/dashes ignored.
    /// Returns null for blank input; throws for a value that is present but malformed.
    public static string? NormalizeInn(string? inn)
    {
        if (string.IsNullOrWhiteSpace(inn)) return null;

        var cleaned = new string(inn.Where(char.IsDigit).ToArray());
        if (!InnPattern.IsMatch(cleaned))
            throw new AppException("INN (STIR) must be exactly 9 digits");
        return cleaned;
    }

    /// Finds the organization with this INN or creates it. Returns null when no INN is given.
    public static async Task<Organization?> ResolveAsync(WmsDbContext db, string? inn, string name,
        string? phone = null, string? address = null, CancellationToken ct = default)
    {
        var normalized = NormalizeInn(inn);
        if (normalized == null) return null;

        var existing = await db.Organizations.FirstOrDefaultAsync(o => o.Inn == normalized, ct);
        if (existing != null)
        {
            // Fill in blanks from whoever knows more, but never overwrite existing values:
            // one tenant's sloppy data must not damage what another tenant entered.
            if (string.IsNullOrWhiteSpace(existing.Phone) && !string.IsNullOrWhiteSpace(phone))
                existing.Phone = phone;
            if (string.IsNullOrWhiteSpace(existing.Address) && !string.IsNullOrWhiteSpace(address))
                existing.Address = address;
            return existing;
        }

        var organization = new Organization
        {
            Name = name.Trim(),
            Inn = normalized,
            Phone = phone,
            Address = address
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);
        return organization;
    }
}
