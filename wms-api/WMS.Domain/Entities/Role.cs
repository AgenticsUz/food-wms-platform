using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Role : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
