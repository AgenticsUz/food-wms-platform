using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Vehicle : TenantEntity
{
    /// <summary>Raqam yoki nom.</summary>
    public string Name { get; set; } = null!;
    public string? Model { get; set; }

    /// <summary>kg yoki birlik.</summary>
    public decimal Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Driver : TenantEntity
{
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Delivery : TenantEntity
{
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Planned;
    public DateTime ScheduledDate { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public UserProfile? CreatedByUser { get; set; }
    public ICollection<DeliveryStop> Stops { get; set; } = new List<DeliveryStop>();
}

public class DeliveryStop : TenantEntity
{
    public Guid DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;
    public Guid CounterpartyId { get; set; }
    public Counterparty Counterparty { get; set; } = null!;

    /// <summary>Yetkazilayotgan sotuv (ixtiyoriy).</summary>
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public string? Address { get; set; }
    public int SequenceOrder { get; set; }
    public DeliveryStopStatus Status { get; set; } = DeliveryStopStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public string? Note { get; set; }
}
