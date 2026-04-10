using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class ShiftPlan : BaseEntity
{
    public int TenantId { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public DateTime Date { get; set; }
}
