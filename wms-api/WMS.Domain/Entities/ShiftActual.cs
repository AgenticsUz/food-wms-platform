using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class ShiftActual : BaseEntity
{
    public int TenantId { get; set; }
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; } = 0;
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}
