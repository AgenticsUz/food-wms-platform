using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class ProductionRecipe : BaseEntity
{
    public int TenantId { get; set; }
    public int OutputProductId { get; set; }
    public Product OutputProduct { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal OutputQuantity { get; set; }
    public int OutputUnitId { get; set; }
    public Unit OutputUnit { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeStage> RecipeStages { get; set; } = new List<RecipeStage>();
}
