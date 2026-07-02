using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Delivery;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly WmsDbContext _db;
    public DeliveryService(WmsDbContext db) => _db = db;

    // ── Vehicles ──

    public async Task<List<VehicleDto>> GetVehiclesAsync(int tenantId)
    {
        return await _db.Vehicles
            .Where(v => v.TenantId == tenantId)
            .OrderByDescending(v => v.IsActive).ThenBy(v => v.Name)
            .Select(v => new VehicleDto
            {
                Id = v.Id, Name = v.Name, Model = v.Model,
                Capacity = v.Capacity, IsActive = v.IsActive
            })
            .ToListAsync();
    }

    public async Task<VehicleDto> CreateVehicleAsync(int tenantId, CreateVehicleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Vehicle name is required");

        var v = new Vehicle
        {
            TenantId = tenantId, Name = dto.Name.Trim(), Model = dto.Model,
            Capacity = dto.Capacity, IsActive = dto.IsActive
        };
        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();
        return MapVehicle(v);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(int tenantId, int id, CreateVehicleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Vehicle name is required");

        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Vehicle not found");
        v.Name = dto.Name.Trim();
        v.Model = dto.Model;
        v.Capacity = dto.Capacity;
        v.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return MapVehicle(v);
    }

    public async Task DeleteVehicleAsync(int tenantId, int id)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Vehicle not found");
        v.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static VehicleDto MapVehicle(Vehicle v) => new()
    {
        Id = v.Id, Name = v.Name, Model = v.Model,
        Capacity = v.Capacity, IsActive = v.IsActive
    };

    // ── Drivers ──

    public async Task<List<DriverDto>> GetDriversAsync(int tenantId)
    {
        return await _db.Drivers
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.IsActive).ThenBy(d => d.FullName)
            .Select(d => new DriverDto
            {
                Id = d.Id, FullName = d.FullName, Phone = d.Phone,
                LicenseNumber = d.LicenseNumber, IsActive = d.IsActive
            })
            .ToListAsync();
    }

    public async Task<DriverDto> CreateDriverAsync(int tenantId, CreateDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new AppException("Driver name is required");

        var d = new Driver
        {
            TenantId = tenantId, FullName = dto.FullName.Trim(), Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber, IsActive = dto.IsActive
        };
        _db.Drivers.Add(d);
        await _db.SaveChangesAsync();
        return MapDriver(d);
    }

    public async Task<DriverDto> UpdateDriverAsync(int tenantId, int id, CreateDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new AppException("Driver name is required");

        var d = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Driver not found");
        d.FullName = dto.FullName.Trim();
        d.Phone = dto.Phone;
        d.LicenseNumber = dto.LicenseNumber;
        d.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return MapDriver(d);
    }

    public async Task DeleteDriverAsync(int tenantId, int id)
    {
        var d = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Driver not found");
        d.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static DriverDto MapDriver(Driver d) => new()
    {
        Id = d.Id, FullName = d.FullName, Phone = d.Phone,
        LicenseNumber = d.LicenseNumber, IsActive = d.IsActive
    };

    // ── Deliveries ──

    public async Task<List<DeliveryDto>> GetDeliveriesAsync(int tenantId, DeliveryStatus? status)
    {
        var q = _db.Deliveries.Where(d => d.TenantId == tenantId);
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);

        var deliveries = await q
            .Include(d => d.Vehicle).Include(d => d.Driver).Include(d => d.CreatedByUser)
            .Include(d => d.Stops).ThenInclude(s => s.Counterparty)
            .OrderByDescending(d => d.ScheduledDate).ThenByDescending(d => d.Id)
            .ToListAsync();

        return deliveries.Select(MapDelivery).ToList();
    }

    public async Task<DeliveryDto> GetDeliveryByIdAsync(int tenantId, int id)
        => MapDelivery(await GetDeliveryEntity(tenantId, id));

    public async Task<DeliveryDto> CreateDeliveryAsync(int tenantId, int userId, CreateDeliveryDto dto)
    {
        await ValidateCreateAsync(tenantId, dto);

        var delivery = new Delivery
        {
            TenantId = tenantId,
            VehicleId = dto.VehicleId,
            DriverId = dto.DriverId,
            ScheduledDate = dto.ScheduledDate,
            Note = dto.Note,
            CreatedByUserId = userId,
            Status = DeliveryStatus.Planned
        };

        foreach (var stop in dto.Stops)
        {
            delivery.Stops.Add(new DeliveryStop
            {
                TenantId = tenantId,
                CounterpartyId = stop.CounterpartyId,
                TransferId = stop.TransferId,
                Address = stop.Address,
                SequenceOrder = stop.SequenceOrder,
                Note = stop.Note,
                Status = DeliveryStopStatus.Pending
            });
        }

        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync();

        return await GetDeliveryByIdAsync(tenantId, delivery.Id);
    }

    public async Task<DeliveryDto> UpdateStatusAsync(int tenantId, int id, UpdateDeliveryStatusDto dto)
    {
        var delivery = await GetDeliveryEntity(tenantId, id);
        delivery.Status = dto.Status;
        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    public async Task DeleteDeliveryAsync(int tenantId, int id)
    {
        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId)
            ?? throw new NotFoundException("Delivery not found");
        if (delivery.Status != DeliveryStatus.Planned)
            throw new AppException("Only planned deliveries can be deleted");
        delivery.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Stops ──

    public async Task<DeliveryDto> MarkStopDeliveredAsync(int tenantId, int deliveryId, int stopId)
    {
        var delivery = await GetDeliveryEntity(tenantId, deliveryId);
        var stop = delivery.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new NotFoundException("Delivery stop not found");

        stop.Status = DeliveryStopStatus.Delivered;
        stop.DeliveredAt = DateTime.UtcNow;

        // Move the delivery forward: first delivered stop starts it,
        // all stops resolved (delivered/failed) with at least one delivered completes it.
        if (delivery.Status == DeliveryStatus.Planned)
            delivery.Status = DeliveryStatus.InProgress;

        if (delivery.Stops.All(s => s.Status != DeliveryStopStatus.Pending)
            && delivery.Stops.Any(s => s.Status == DeliveryStopStatus.Delivered))
            delivery.Status = DeliveryStatus.Completed;

        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    public async Task<DeliveryDto> MarkStopFailedAsync(int tenantId, int deliveryId, int stopId, string? note)
    {
        var delivery = await GetDeliveryEntity(tenantId, deliveryId);
        var stop = delivery.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new NotFoundException("Delivery stop not found");

        stop.Status = DeliveryStopStatus.Failed;
        stop.DeliveredAt = null;
        if (!string.IsNullOrWhiteSpace(note)) stop.Note = note;

        if (delivery.Status == DeliveryStatus.Planned)
            delivery.Status = DeliveryStatus.InProgress;

        if (delivery.Stops.All(s => s.Status != DeliveryStopStatus.Pending)
            && delivery.Stops.Any(s => s.Status == DeliveryStopStatus.Delivered))
            delivery.Status = DeliveryStatus.Completed;

        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    // ── Validation ──

    private async Task ValidateCreateAsync(int tenantId, CreateDeliveryDto dto)
    {
        if (dto.Stops == null || dto.Stops.Count == 0)
            throw new AppException("Delivery must contain at least one stop");

        if (dto.VehicleId.HasValue &&
            !await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId.Value && v.TenantId == tenantId))
            throw new NotFoundException("Vehicle not found");

        if (dto.DriverId.HasValue &&
            !await _db.Drivers.AnyAsync(d => d.Id == dto.DriverId.Value && d.TenantId == tenantId))
            throw new NotFoundException("Driver not found");

        var counterpartyIds = dto.Stops.Select(s => s.CounterpartyId).Distinct().ToList();
        var cpCount = await _db.Counterparties
            .CountAsync(c => c.TenantId == tenantId && counterpartyIds.Contains(c.Id));
        if (cpCount != counterpartyIds.Count) throw new NotFoundException("Counterparty not found");

        var transferIds = dto.Stops.Where(s => s.TransferId.HasValue)
            .Select(s => s.TransferId!.Value).Distinct().ToList();
        if (transferIds.Count > 0)
        {
            var tCount = await _db.Transfers
                .CountAsync(t => t.TenantId == tenantId && transferIds.Contains(t.Id));
            if (tCount != transferIds.Count) throw new NotFoundException("Transfer not found");
        }
    }

    // ── Helpers ──

    private async Task<Delivery> GetDeliveryEntity(int tenantId, int id)
    {
        return await _db.Deliveries
            .Include(d => d.Vehicle).Include(d => d.Driver).Include(d => d.CreatedByUser)
            .Include(d => d.Stops).ThenInclude(s => s.Counterparty)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId)
            ?? throw new NotFoundException("Delivery not found");
    }

    private static DeliveryDto MapDelivery(Delivery d)
    {
        var stops = d.Stops.OrderBy(s => s.SequenceOrder).ThenBy(s => s.Id).ToList();
        return new DeliveryDto
        {
            Id = d.Id,
            VehicleId = d.VehicleId, VehicleName = d.Vehicle?.Name,
            DriverId = d.DriverId, DriverName = d.Driver?.FullName,
            Status = d.Status,
            ScheduledDate = d.ScheduledDate,
            Note = d.Note,
            CreatedByUserName = d.CreatedByUser?.FullName,
            CreatedAt = d.CreatedAt,
            StopCount = stops.Count,
            DeliveredCount = stops.Count(s => s.Status == DeliveryStopStatus.Delivered),
            Stops = stops.Select(s => new DeliveryStopDto
            {
                Id = s.Id,
                CounterpartyId = s.CounterpartyId,
                CounterpartyName = s.Counterparty?.Name,
                TransferId = s.TransferId,
                Address = s.Address,
                SequenceOrder = s.SequenceOrder,
                Status = s.Status,
                DeliveredAt = s.DeliveredAt,
                Note = s.Note
            }).ToList()
        };
    }
}
