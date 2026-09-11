using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

public class WarehouseService : IWarehouseService
{
    private readonly WmsDbContext _db;
    private readonly IRequestWarnings _warnings;
    private readonly INotificationService _notifications;
    private readonly SubscriptionOptions _subscription;

    public WarehouseService(WmsDbContext db, IRequestWarnings warnings, IOptions<SubscriptionOptions> subscription,
        INotificationService notifications)
    {
        _db = db;
        _warnings = warnings;
        _notifications = notifications;
        _subscription = subscription.Value;
    }

    public async Task<List<WarehouseDto>> GetAllAsync()
    {
        return await _db.Warehouses
            .Select(w => new WarehouseDto
            {
                Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description
            }).ToListAsync();
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto)
    {
        await PlanLimits.EnsureCanAddWarehouseAsync(_db);

        var w = new Warehouse
        {
            Name = dto.Name, Type = dto.Type, Description = dto.Description
        };
        _db.Warehouses.Add(w);
        await _db.SaveChangesAsync();

        await PlanLimits.ReportUsageAsync(_db, _warnings, PlanLimits.Warehouses, _subscription.LimitWarnPercent, _notifications);

        return new WarehouseDto { Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description };
    }

    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto dto)
    {
        var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Warehouse not found");
        w.Name = dto.Name; w.Type = dto.Type; w.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new WarehouseDto { Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description };
    }

    public async Task DeleteAsync(Guid id)
    {
        var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Warehouse not found");

        var hasStock = await _db.WarehouseStocks.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
        if (hasStock)
            throw new AppException("Cannot delete warehouse: it still has stock");

        w.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<StockDto>> GetStockAsync(Guid warehouseId)
    {
        // Yig'indilar SQL'da (GROUP BY): SQLite davrida decimal double sifatida saqlangani uchun
        // hamma qator xotiraga tortilib yig'ilardi. Mahsulot nomi va birligi JOIN bilan —
        // o'chirilgan mahsulot qoldig'i eski Include kabi ro'yxatga chiqmaydi.
        var totals = _db.WarehouseStocks
            .Where(s => s.WarehouseId == warehouseId)
            .GroupBy(s => s.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Total = g.Sum(s => s.Quantity),
                Reserved = g.Sum(s => s.ReservedQuantity)
            });

        return await (
            from t in totals
            join p in _db.Products on t.ProductId equals p.Id
            select new StockDto
            {
                ProductId = t.ProductId,
                ProductName = p.Name,
                UnitShortName = p.Unit.ShortName,
                TotalQuantity = t.Total,
                ReservedQuantity = t.Reserved,
                AvailableQuantity = t.Total - t.Reserved
            }).ToListAsync();
    }

    public async Task<List<StockDetailDto>> GetStockDetailAsync(Guid warehouseId)
    {
        return await _db.WarehouseStocks
            .Where(s => s.WarehouseId == warehouseId)
            .Select(s => new StockDetailDto
            {
                ProductId = s.ProductId, ProductName = s.Product.Name,
                UnitShortName = s.Product.Unit.ShortName,
                BatchId = s.BatchId, LotNumber = s.Batch.LotNumber,
                ExpiryDate = s.Batch.ExpiryDate,
                LocationId = s.LocationId, LocationName = s.Location.Name,
                Quantity = s.Quantity, ReservedQuantity = s.ReservedQuantity
            }).ToListAsync();
    }

    public async Task<List<LocationDto>> GetLocationsAsync(Guid? warehouseId = null)
    {
        var q = _db.Locations.AsQueryable();
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);

        return await q.Select(l => new LocationDto
        {
            Id = l.Id, WarehouseId = l.WarehouseId, WarehouseName = l.Warehouse.Name,
            Name = l.Name, Code = l.Code
        }).ToListAsync();
    }

    public async Task<LocationDto> CreateLocationAsync(CreateLocationDto dto)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId)
            ?? throw new NotFoundException("Warehouse not found");
        var loc = new Location { WarehouseId = dto.WarehouseId, Name = dto.Name, Code = dto.Code };
        _db.Locations.Add(loc);
        await _db.SaveChangesAsync();
        return new LocationDto
        {
            Id = loc.Id, WarehouseId = loc.WarehouseId, WarehouseName = warehouse.Name,
            Name = loc.Name, Code = loc.Code
        };
    }

    public async Task<LocationDto> UpdateLocationAsync(Guid id, CreateLocationDto dto)
    {
        var loc = await _db.Locations.Include(l => l.Warehouse)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Location not found");

        loc.Name = dto.Name;
        loc.Code = dto.Code;
        await _db.SaveChangesAsync();

        return new LocationDto
        {
            Id = loc.Id, WarehouseId = loc.WarehouseId, WarehouseName = loc.Warehouse.Name,
            Name = loc.Name, Code = loc.Code
        };
    }

    public async Task DeleteLocationAsync(Guid id)
    {
        var loc = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new NotFoundException("Location not found");

        var hasStock = await _db.WarehouseStocks.AnyAsync(s => s.LocationId == id && s.Quantity > 0);
        if (hasStock)
            throw new AppException("Cannot delete location: it still has stock");

        loc.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<BatchDto>> GetBatchesAsync()
    {
        return await _db.Batches
            .OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
            .Select(b => new BatchDto
            {
                Id = b.Id, ProductId = b.ProductId, ProductName = b.Product.Name,
                LotNumber = b.LotNumber, ManufacturedDate = b.ManufacturedDate,
                ExpiryDate = b.ExpiryDate, InitialQuantity = b.InitialQuantity,
                RemainingQuantity = b.RemainingQuantity, Notes = b.Notes
            }).ToListAsync();
    }

    public async Task<BatchDto> UpdateBatchAsync(Guid id, UpdateBatchDto dto)
    {
        var b = await _db.Batches.Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Batch not found");

        if (string.IsNullOrWhiteSpace(dto.LotNumber))
            throw new AppException("LotNumber is required");

        var lotExists = await _db.Batches.AnyAsync(x =>
            x.ProductId == b.ProductId && x.LotNumber == dto.LotNumber && x.Id != id);
        if (lotExists) throw new AppException("LotNumber already exists for this product");

        b.LotNumber = dto.LotNumber;
        b.ExpiryDate = dto.ExpiryDate;
        b.Notes = dto.Notes;
        await _db.SaveChangesAsync();

        return new BatchDto
        {
            Id = b.Id, ProductId = b.ProductId, ProductName = b.Product.Name,
            LotNumber = b.LotNumber, ManufacturedDate = b.ManufacturedDate,
            ExpiryDate = b.ExpiryDate, InitialQuantity = b.InitialQuantity,
            RemainingQuantity = b.RemainingQuantity, Notes = b.Notes
        };
    }

    public async Task DeleteBatchAsync(Guid id)
    {
        var b = await _db.Batches.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Batch not found");

        var hasStock = await _db.WarehouseStocks
            .AnyAsync(s => s.BatchId == id && s.Quantity > 0);
        if (hasStock)
            throw new AppException("Cannot delete batch: it has remaining stock in warehouse");

        b.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}
