using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class Batch : BaseEntity
{
    public int TenantId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LotNumber { get; set; } = null!;
    public DateTime ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public string? Notes { get; set; }
}
