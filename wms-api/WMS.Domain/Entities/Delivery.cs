using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Delivery : BaseEntity
{
    public int TenantId { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Planned;
    public DateTime ScheduledDate { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<DeliveryStop> Stops { get; set; } = new List<DeliveryStop>();
}
