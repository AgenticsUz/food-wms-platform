using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Warehouses;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class WarehouseService : IWarehouseService
{
    private readonly WmsDbContext _db;
    public WarehouseService(WmsDbContext db) => _db = db;

    public async Task<List<WarehouseDto>> GetAllAsync(int tenantId)
    {
        return await _db.Warehouses.Where(w => w.TenantId == tenantId)
            .Select(w => new WarehouseDto
            {
                Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description
            }).ToListAsync();
    }

    public async Task<WarehouseDto> CreateAsync(int tenantId, CreateWarehouseDto dto)
    {
        var w = new Warehouse
        {
            TenantId = tenantId, Name = dto.Name, Type = dto.Type, Description = dto.Description
        };
        _db.Warehouses.Add(w);
        await _db.SaveChangesAsync();
        return new WarehouseDto { Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description };
    }

    public async Task<WarehouseDto> UpdateAsync(int tenantId, int id, UpdateWarehouseDto dto)
    {
        var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Warehouse not found");
        w.Name = dto.Name; w.Type = dto.Type; w.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new WarehouseDto { Id = w.Id, Name = w.Name, Type = w.Type, Description = w.Description };
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var w = await _db.Warehouses.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Warehouse not found");

        var hasStock = await _db.WarehouseStocks.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
        if (hasStock)
            throw new AppException("Cannot delete warehouse: it still has stock");

        w.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<StockDto>> GetStockAsync(int tenantId, int warehouseId)
    {
        var stocks = await _db.WarehouseStocks
            .Where(s => s.TenantId == tenantId && s.WarehouseId == warehouseId)
            .Include(s => s.Product).ThenInclude(p => p.Unit)
            .ToListAsync();

        return stocks
            .GroupBy(s => s.ProductId)
            .Select(g =>
            {
                var first = g.First();
                return new StockDto
                {
                    ProductId = g.Key,
                    ProductName = first.Product.Name,
                    UnitShortName = first.Product.Unit.ShortName,
                    TotalQuantity = g.Sum(s => s.Quantity),
                    ReservedQuantity = g.Sum(s => s.ReservedQuantity),
                    AvailableQuantity = g.Sum(s => s.Quantity - s.ReservedQuantity)
                };
            }).ToList();
    }

    public async Task<List<StockDetailDto>> GetStockDetailAsync(int tenantId, int warehouseId)
    {
        return await _db.WarehouseStocks
            .Where(s => s.TenantId == tenantId && s.WarehouseId == warehouseId)
            .Include(s => s.Product).ThenInclude(p => p.Unit)
            .Include(s => s.Batch).Include(s => s.Location)
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

    public async Task<List<LocationDto>> GetLocationsAsync(int tenantId, int? warehouseId = null)
    {
        var q = _db.Locations.Include(l => l.Warehouse)
            .Where(l => l.Warehouse.TenantId == tenantId);
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);

        return await q.Select(l => new LocationDto
        {
            Id = l.Id, WarehouseId = l.WarehouseId, WarehouseName = l.Warehouse.Name,
            Name = l.Name, Code = l.Code
        }).ToListAsync();
    }

    public async Task<LocationDto> CreateLocationAsync(int tenantId, CreateLocationDto dto)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId && w.TenantId == tenantId)
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

    public async Task<List<BatchDto>> GetBatchesAsync(int tenantId)
    {
        return await _db.Batches.Where(b => b.TenantId == tenantId)
            .Include(b => b.Product)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BatchDto
            {
                Id = b.Id, ProductId = b.ProductId, ProductName = b.Product.Name,
                LotNumber = b.LotNumber, ManufacturedDate = b.ManufacturedDate,
                ExpiryDate = b.ExpiryDate, InitialQuantity = b.InitialQuantity,
                RemainingQuantity = b.RemainingQuantity, Notes = b.Notes
            }).ToListAsync();
    }

    public async Task<BatchDto> UpdateBatchAsync(int tenantId, int id, UpdateBatchDto dto)
    {
        var b = await _db.Batches.Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Batch not found");

        if (string.IsNullOrWhiteSpace(dto.LotNumber))
            throw new AppException("LotNumber is required");

        var lotExists = await _db.Batches.AnyAsync(x =>
            x.TenantId == tenantId && x.ProductId == b.ProductId &&
            x.LotNumber == dto.LotNumber && x.Id != id);
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

    public async Task DeleteBatchAsync(int tenantId, int id)
    {
        var b = await _db.Batches.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Batch not found");

        var hasStock = await _db.WarehouseStocks
            .AnyAsync(s => s.BatchId == id && s.Quantity > 0);
        if (hasStock)
            throw new AppException("Cannot delete batch: it has remaining stock in warehouse");

        b.IsDeleted = true;
        await _db.SaveChangesAsync();
    }
}
