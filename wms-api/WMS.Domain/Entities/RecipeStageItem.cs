using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class RecipeStageItem : BaseEntity
{
    public int RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
}
