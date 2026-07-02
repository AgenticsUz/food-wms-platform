using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Driver : BaseEntity
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
