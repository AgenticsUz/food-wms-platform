using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Category : BaseEntity
{
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
