using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class RecipeStage : BaseEntity
{
    public int RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; } = null!;
    public int StageId { get; set; }
    public ProductionStage Stage { get; set; } = null!;
    public int OrderNumber { get; set; }
    public int? OutputProductId { get; set; }
    public Product? OutputProduct { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; } = false;
    public int? OutputWarehouseId { get; set; }
    public Warehouse? OutputWarehouse { get; set; }
    public ICollection<RecipeStageItem> Inputs { get; set; } = new List<RecipeStageItem>();
}
