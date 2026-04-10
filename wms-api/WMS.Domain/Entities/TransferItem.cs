using System.ComponentModel.DataAnnotations.Schema;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class TransferItem : BaseEntity
{
    public int TransferId { get; set; }
    public Transfer Transfer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal TotalPrice => Quantity * UnitPrice;
}
