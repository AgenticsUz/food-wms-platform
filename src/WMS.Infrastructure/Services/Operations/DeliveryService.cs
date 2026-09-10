using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Delivery;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations;

// F6: tenant parametri va `TenantId == tenantId` shartlari o'chdi (D4) — WmsDbContext filtri
// va RLS o'qishni, StampEntries esa yangi yozuvning tenantini o'zi hal qiladi.
public class DeliveryService : IDeliveryService
{
    private readonly WmsDbContext _db;
    public DeliveryService(WmsDbContext db) => _db = db;

    // ── Vehicles ──

    public async Task<List<VehicleDto>> GetVehiclesAsync()
    {
        return await _db.Vehicles
            .OrderByDescending(v => v.IsActive).ThenBy(v => v.Name)
            .Select(v => new VehicleDto
            {
                Id = v.Id, Name = v.Name, Model = v.Model,
                Capacity = v.Capacity, IsActive = v.IsActive
            })
            .ToListAsync();
    }

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Vehicle name is required");

        var v = new Vehicle
        {
            Name = dto.Name.Trim(), Model = dto.Model,
            Capacity = dto.Capacity, IsActive = dto.IsActive
        };
        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();
        return MapVehicle(v);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(Guid id, CreateVehicleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new AppException("Vehicle name is required");

        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Vehicle not found");
        v.Name = dto.Name.Trim();
        v.Model = dto.Model;
        v.Capacity = dto.Capacity;
        v.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return MapVehicle(v);
    }

    public async Task DeleteVehicleAsync(Guid id)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id)
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

    public async Task<List<DriverDto>> GetDriversAsync()
    {
        return await _db.Drivers
            .OrderByDescending(d => d.IsActive).ThenBy(d => d.FullName)
            .Select(d => new DriverDto
            {
                Id = d.Id, FullName = d.FullName, Phone = d.Phone,
                LicenseNumber = d.LicenseNumber, IsActive = d.IsActive
            })
            .ToListAsync();
    }

    public async Task<DriverDto> CreateDriverAsync(CreateDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new AppException("Driver name is required");

        var d = new Driver
        {
            FullName = dto.FullName.Trim(), Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber, IsActive = dto.IsActive
        };
        _db.Drivers.Add(d);
        await _db.SaveChangesAsync();
        return MapDriver(d);
    }

    public async Task<DriverDto> UpdateDriverAsync(Guid id, CreateDriverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new AppException("Driver name is required");

        var d = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Driver not found");
        d.FullName = dto.FullName.Trim();
        d.Phone = dto.Phone;
        d.LicenseNumber = dto.LicenseNumber;
        d.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return MapDriver(d);
    }

    public async Task DeleteDriverAsync(Guid id)
    {
        var d = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == id)
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

    public async Task<List<DeliveryDto>> GetDeliveriesAsync(DeliveryStatus? status)
    {
        var q = _db.Deliveries.AsQueryable();
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);

        // Split query: har yetkazishning bir nechta to'xtashi bor — bitta JOIN'da
        // Vehicle/Driver/CreatedByUser ustunlari har to'xtash uchun takrorlanardi.
        var deliveries = await q
            .AsNoTracking()
            .Include(d => d.Vehicle).Include(d => d.Driver).Include(d => d.CreatedByUser)
            .Include(d => d.Stops).ThenInclude(s => s.Counterparty)
            .AsSplitQuery()
            .OrderByDescending(d => d.ScheduledDate).ThenByDescending(d => d.Id)
            .ToListAsync();

        return deliveries.Select(MapDelivery).ToList();
    }

    public async Task<DeliveryDto> GetDeliveryByIdAsync(Guid id)
        => MapDelivery(await GetDeliveryEntity(id));

    public async Task<DeliveryDto> CreateDeliveryAsync(Guid userId, CreateDeliveryDto dto)
    {
        await ValidateCreateAsync(dto);

        var delivery = new Delivery
        {
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

        return await GetDeliveryByIdAsync(delivery.Id);
    }

    public async Task<DeliveryDto> UpdateStatusAsync(Guid id, UpdateDeliveryStatusDto dto)
    {
        var delivery = await GetDeliveryEntity(id);
        delivery.Status = dto.Status;
        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    public async Task DeleteDeliveryAsync(Guid id)
    {
        var delivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Delivery not found");
        if (delivery.Status != DeliveryStatus.Planned)
            throw new AppException("Only planned deliveries can be deleted");
        delivery.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Stops ──

    public async Task<DeliveryDto> MarkStopDeliveredAsync(Guid deliveryId, Guid stopId)
    {
        var delivery = await GetDeliveryEntity(deliveryId);
        var stop = delivery.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new NotFoundException("Delivery stop not found");

        stop.Status = DeliveryStopStatus.Delivered;
        stop.DeliveredAt = DateTime.UtcNow;

        AdvanceStatus(delivery);

        // To'xtash va yetkazish holati BITTA SaveChanges'da — yarim yozilgan holat qolmasin.
        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    public async Task<DeliveryDto> MarkStopFailedAsync(Guid deliveryId, Guid stopId, string? note)
    {
        var delivery = await GetDeliveryEntity(deliveryId);
        var stop = delivery.Stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new NotFoundException("Delivery stop not found");

        stop.Status = DeliveryStopStatus.Failed;
        stop.DeliveredAt = null;
        if (!string.IsNullOrWhiteSpace(note)) stop.Note = note;

        AdvanceStatus(delivery);

        await _db.SaveChangesAsync();
        return MapDelivery(delivery);
    }

    // Move the delivery forward: first resolved stop starts it,
    // all stops resolved (delivered/failed) with at least one delivered completes it.
    private static void AdvanceStatus(Delivery delivery)
    {
        if (delivery.Status == DeliveryStatus.Planned)
            delivery.Status = DeliveryStatus.InProgress;

        if (delivery.Stops.All(s => s.Status != DeliveryStopStatus.Pending)
            && delivery.Stops.Any(s => s.Status == DeliveryStopStatus.Delivered))
            delivery.Status = DeliveryStatus.Completed;
    }

    // ── Validation ──

    private async Task ValidateCreateAsync(CreateDeliveryDto dto)
    {
        if (dto.Stops == null || dto.Stops.Count == 0)
            throw new AppException("Delivery must contain at least one stop");

        if (dto.VehicleId.HasValue &&
            !await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId.Value))
            throw new NotFoundException("Vehicle not found");

        if (dto.DriverId.HasValue &&
            !await _db.Drivers.AnyAsync(d => d.Id == dto.DriverId.Value))
            throw new NotFoundException("Driver not found");

        // Begona tenantning id'si filtr/RLS ostida «topilmadi» bo'ladi — FK xatosi (500) o'rniga 404.
        var counterpartyIds = dto.Stops.Select(s => s.CounterpartyId).Distinct().ToList();
        var cpCount = await _db.Counterparties
            .CountAsync(c => counterpartyIds.Contains(c.Id));
        if (cpCount != counterpartyIds.Count) throw new NotFoundException("Counterparty not found");

        var transferIds = dto.Stops.Where(s => s.TransferId.HasValue)
            .Select(s => s.TransferId!.Value).Distinct().ToList();
        if (transferIds.Count > 0)
        {
            var tCount = await _db.Transfers
                .CountAsync(t => transferIds.Contains(t.Id));
            if (tCount != transferIds.Count) throw new NotFoundException("Transfer not found");
        }
    }

    // ── Helpers ──

    private async Task<Delivery> GetDeliveryEntity(Guid id)
    {
        return await _db.Deliveries
            .Include(d => d.Vehicle).Include(d => d.Driver).Include(d => d.CreatedByUser)
            .Include(d => d.Stops).ThenInclude(s => s.Counterparty)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new NotFoundException("Delivery not found");
    }

    private static DeliveryDto MapDelivery(Delivery d)
    {
        // Guid v7 vaqt bo'yicha o'sadi — Id bo'yicha tartib SQLite'dagi int kabi yaratilish tartibi.
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
