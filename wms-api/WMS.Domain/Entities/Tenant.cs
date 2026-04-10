using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TenantModule> TenantModules { get; set; } = new List<TenantModule>();
}
