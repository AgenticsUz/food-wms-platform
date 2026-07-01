using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class TransferService : ITransferService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;
    public TransferService(WmsDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task<List<TransferDto>> GetAllAsync(int tenantId, TransferType? type,
        TransferStatus? status, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var q = _db.Transfers.Where(t => t.TenantId == tenantId)
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Agent)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .AsQueryable();

        if (type.HasValue) q = q.Where(t => t.Type == type.Value);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (from.HasValue) q = q.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(t => t.CreatedAt <= to.Value);

        var transfers = await q.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return transfers.Select(t => MapToDto(t)).ToList();
    }

    public async Task<TransferDto> GetByIdAsync(int tenantId, int id)
    {
        var t = await GetTransferEntity(tenantId, id);
        return MapToDto(t);
    }

    public async Task<TransferDto> CreateAsync(int tenantId, int userId, CreateTransferDto dto)
    {
        var transfer = new Transfer
        {
            TenantId = tenantId, Type = dto.Type, FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId, CounterpartyId = dto.CounterpartyId,
            AgentId = dto.AgentId, CommissionPercent = dto.CommissionPercent,
            ReturnReason = dto.ReturnReason, OriginalTransferId = dto.OriginalTransferId,
            CreatedByUserId = userId, Note = dto.Note, Status = TransferStatus.Pending
        };

        foreach (var item in dto.Items)
        {
            transfer.Items.Add(new TransferItem
            {
                ProductId = item.ProductId, BatchId = item.BatchId,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice
            });
        }

        _db.Transfers.Add(transfer);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(tenantId, transfer.Id);
    }

    public async Task<TransferDto> ConfirmAsync(int tenantId, int id)
    {
        var transfer = await GetTransferEntity(tenantId, id);
        if (transfer.Status != TransferStatus.Pending)
            throw new Exception("Only pending transfers can be confirmed");

        switch (transfer.Type)
        {
            case TransferType.Incoming:
                await ProcessIncoming(transfer, tenantId);
                break;
            case TransferType.Outgoing:
                await ProcessOutgoing(transfer, tenantId);
                break;
            case TransferType.Internal:
                await ProcessInternal(transfer, tenantId);
                break;
            case TransferType.Return:
                await ProcessReturn(transfer, tenantId);
                break;
        }

        transfer.Status = TransferStatus.Confirmed;
        transfer.ConfirmedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Update debt
        await UpdateDebt(transfer, tenantId);

        // Record agent commission (sale via agent)
        await CreateCommission(transfer, tenantId);

        // Cancel commission of the original sale when a return is confirmed
        await CancelCommissionForReturn(transfer, tenantId);

        await _db.SaveChangesAsync();

        // Check low stock after outgoing
        if (transfer.Type == TransferType.Outgoing || transfer.Type == TransferType.Internal)
            await CheckLowStock(transfer, tenantId);

        var totalAmount = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);

        if (transfer.Type == TransferType.Return)
        {
            await _notifications.CreateAsync(tenantId, null,
                "Return Received",
                $"Return #{transfer.Id} received from {transfer.Counterparty?.Name}. Amount: {totalAmount:N0}",
                NotificationType.Info, "Transfer", transfer.Id);
        }
        else
        {
            await _notifications.CreateAsync(tenantId, null,
                "Transfer Confirmed",
                $"Transfer #{transfer.Id} has been confirmed. Amount: {totalAmount:N0}",
                NotificationType.TransferConfirmed, "Transfer", transfer.Id);
        }

        return MapToDto(transfer);
    }

    public async Task<TransferDto> RejectAsync(int tenantId, int id)
    {
        var transfer = await GetTransferEntity(tenantId, id);
        if (transfer.Status != TransferStatus.Pending)
            throw new Exception("Only pending transfers can be rejected");
        transfer.Status = TransferStatus.Rejected;
        await _db.SaveChangesAsync();

        await _notifications.CreateAsync(tenantId, null,
            "Transfer Rejected",
            $"Transfer #{transfer.Id} has been rejected.",
            NotificationType.TransferRejected, "Transfer", transfer.Id);

        return MapToDto(transfer);
    }

    public async Task CancelAsync(int tenantId, int id)
    {
        var transfer = await _db.Transfers.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId)
            ?? throw new Exception("Transfer not found");
        if (transfer.Status != TransferStatus.Pending)
            throw new Exception("Only pending transfers can be cancelled");
        transfer.Status = TransferStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    private async Task ProcessIncoming(Transfer transfer, int tenantId)
    {
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new Exception("Incoming transfer must have a destination warehouse");

        // Get or create default location in warehouse
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == toWarehouseId);
        if (location == null)
        {
            location = new Location
            {
                WarehouseId = toWarehouseId,
                Name = "Default",
                Code = "DEF"
            };
            _db.Locations.Add(location);
            await _db.SaveChangesAsync();
        }

        foreach (var item in transfer.Items)
        {
            // Create batch
            var product = await _db.Products.FindAsync(item.ProductId);
            var batch = new Batch
            {
                TenantId = tenantId, ProductId = item.ProductId,
                LotNumber = $"LOT-{DateTime.UtcNow:yyyyMMdd}-{item.ProductId}-{Guid.NewGuid().ToString()[..6]}",
                ManufacturedDate = DateTime.UtcNow,
                ExpiryDate = product?.ShelfLifeDays != null
                    ? DateTime.UtcNow.AddDays(product.ShelfLifeDays.Value) : null,
                InitialQuantity = item.Quantity, RemainingQuantity = item.Quantity
            };
            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            item.BatchId = batch.Id;

            // Add to stock and save immediately
            _db.WarehouseStocks.Add(new WarehouseStock
            {
                TenantId = tenantId, WarehouseId = toWarehouseId,
                LocationId = location.Id, ProductId = item.ProductId,
                BatchId = batch.Id, Quantity = item.Quantity
            });
            await _db.SaveChangesAsync();
        }
    }

    private async Task ProcessOutgoing(Transfer transfer, int tenantId)
    {
        var fromWarehouseId = transfer.FromWarehouseId
            ?? throw new Exception("Outgoing transfer must have a source warehouse");

        foreach (var item in transfer.Items)
        {
            var remaining = item.Quantity;

            // FEFO: pick batches with earliest expiry first
            var stocks = await _db.WarehouseStocks
                .Include(s => s.Batch)
                .Where(s => s.TenantId == tenantId && s.WarehouseId == fromWarehouseId
                    && s.ProductId == item.ProductId && s.Quantity > 0)
                .OrderBy(s => s.Batch.ExpiryDate ?? DateTime.MaxValue)
                .ToListAsync();

            foreach (var stock in stocks)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, stock.Quantity);
                stock.Quantity -= take;
                stock.Batch.RemainingQuantity -= take;
                remaining -= take;
            }

            if (remaining > 0)
                throw new Exception($"Insufficient stock for product {item.ProductId}");
        }
    }

    private async Task ProcessInternal(Transfer transfer, int tenantId)
    {
        // Outgoing from source
        await ProcessOutgoing(transfer, tenantId);

        // Incoming to destination (simplified: create new stock records)
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new Exception("Internal transfer must have a destination warehouse");
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == toWarehouseId)
            ?? throw new Exception("No location found in destination warehouse");

        foreach (var item in transfer.Items)
        {
            // Find or create batch at destination
            if (item.BatchId.HasValue)
            {
                var existingStock = await _db.WarehouseStocks.FirstOrDefaultAsync(s =>
                    s.TenantId == tenantId && s.WarehouseId == toWarehouseId
                    && s.ProductId == item.ProductId && s.BatchId == item.BatchId.Value);

                if (existingStock != null)
                {
                    existingStock.Quantity += item.Quantity;
                }
                else
                {
                    _db.WarehouseStocks.Add(new WarehouseStock
                    {
                        TenantId = tenantId, WarehouseId = toWarehouseId,
                        LocationId = location.Id, ProductId = item.ProductId,
                        BatchId = item.BatchId.Value, Quantity = item.Quantity
                    });
                }
            }
        }
    }

    private async Task ProcessReturn(Transfer transfer, int tenantId)
    {
        // Returned goods come back into stock (like Incoming), but flagged with a LOT-RET prefix.
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new Exception("Return transfer must have a destination warehouse");

        // Get or create default location in warehouse
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == toWarehouseId);
        if (location == null)
        {
            location = new Location
            {
                WarehouseId = toWarehouseId,
                Name = "Default",
                Code = "DEF"
            };
            _db.Locations.Add(location);
            await _db.SaveChangesAsync();
        }

        foreach (var item in transfer.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            var batch = new Batch
            {
                TenantId = tenantId, ProductId = item.ProductId,
                LotNumber = $"LOT-RET-{DateTime.UtcNow:yyyyMMdd}-{item.ProductId}-{Guid.NewGuid().ToString()[..6]}",
                ManufacturedDate = DateTime.UtcNow,
                ExpiryDate = product?.ShelfLifeDays != null
                    ? DateTime.UtcNow.AddDays(product.ShelfLifeDays.Value) : null,
                InitialQuantity = item.Quantity, RemainingQuantity = item.Quantity
            };
            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            item.BatchId = batch.Id;

            _db.WarehouseStocks.Add(new WarehouseStock
            {
                TenantId = tenantId, WarehouseId = toWarehouseId,
                LocationId = location.Id, ProductId = item.ProductId,
                BatchId = batch.Id, Quantity = item.Quantity
            });
            await _db.SaveChangesAsync();
        }
    }

    private async Task CancelCommissionForReturn(Transfer transfer, int tenantId)
    {
        if (transfer.Type != TransferType.Return || transfer.OriginalTransferId == null) return;

        var commission = await _db.CommissionRecords.FirstOrDefaultAsync(c =>
            c.TenantId == tenantId
            && c.TransferId == transfer.OriginalTransferId.Value
            && c.Status != CommissionStatus.Cancelled);

        if (commission != null)
            commission.Status = CommissionStatus.Cancelled;
    }

    private async Task UpdateDebt(Transfer transfer, int tenantId)
    {
        if (transfer.CounterpartyId == null) return;

        var totalPrice = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);
        var debt = await _db.Debts.FirstOrDefaultAsync(d =>
            d.TenantId == tenantId && d.CounterpartyId == transfer.CounterpartyId);

        if (debt == null)
        {
            debt = new Debt
            {
                TenantId = tenantId, CounterpartyId = transfer.CounterpartyId.Value, Amount = 0
            };
            _db.Debts.Add(debt);
        }

        switch (transfer.Type)
        {
            case TransferType.Outgoing:
                debt.Amount += totalPrice; // client owes us
                break;
            case TransferType.Incoming:
                debt.Amount -= totalPrice; // we owe supplier
                break;
            case TransferType.Return:
                debt.Amount -= totalPrice; // client returned goods — their debt decreases
                break;
        }
    }

    private async Task CreateCommission(Transfer transfer, int tenantId)
    {
        // Only outgoing sales routed through an agent earn a commission.
        if (transfer.Type != TransferType.Outgoing || transfer.AgentId == null) return;

        // Avoid duplicates if confirm is somehow re-run.
        var exists = await _db.CommissionRecords
            .AnyAsync(c => c.TenantId == tenantId && c.TransferId == transfer.Id);
        if (exists) return;

        var agent = await _db.Agents
            .FirstOrDefaultAsync(a => a.Id == transfer.AgentId.Value && a.TenantId == tenantId);
        if (agent == null) return;

        var saleAmount = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);
        var percent = transfer.CommissionPercent ?? agent.CommissionPercent;
        var commission = Math.Round(saleAmount * percent / 100m, 2);

        _db.CommissionRecords.Add(new CommissionRecord
        {
            TenantId = tenantId,
            AgentId = agent.Id,
            TransferId = transfer.Id,
            SaleAmount = saleAmount,
            CommissionPercent = percent,
            CommissionAmount = commission,
            Status = CommissionStatus.Pending
        });
    }

    private async Task CheckLowStock(Transfer transfer, int tenantId)
    {
        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();
        foreach (var productId in productIds)
        {
            var product = await _db.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || product.MinStock <= 0) continue;

            var currentStock = await _db.WarehouseStocks
                .Where(s => s.TenantId == tenantId && s.ProductId == productId)
                .SumAsync(s => s.Quantity);

            if (currentStock <= product.MinStock)
            {
                var unitName = product.Unit?.ShortName ?? "units";
                await _notifications.CreateAsync(tenantId, null,
                    "Low Stock Alert",
                    $"{product.Name} stock is low ({currentStock:N0} {unitName} remaining). Min: {product.MinStock:N0}",
                    NotificationType.LowStock, "Product", product.Id);
            }
        }
    }

    private async Task<Transfer> GetTransferEntity(int tenantId, int id)
    {
        return await _db.Transfers
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Agent)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .Include(t => t.Items).ThenInclude(i => i.Batch)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId)
            ?? throw new Exception("Transfer not found");
    }

    private static TransferDto MapToDto(Transfer t) => new()
    {
        Id = t.Id, Type = t.Type, Status = t.Status,
        FromWarehouseId = t.FromWarehouseId, FromWarehouseName = t.FromWarehouse?.Name,
        ToWarehouseId = t.ToWarehouseId, ToWarehouseName = t.ToWarehouse?.Name,
        CounterpartyId = t.CounterpartyId, CounterpartyName = t.Counterparty?.Name,
        AgentId = t.AgentId, AgentName = t.Agent?.Name, CommissionPercent = t.CommissionPercent,
        CreatedByUserId = t.CreatedByUserId, CreatedByUserName = t.CreatedByUser?.FullName,
        ReturnReason = t.ReturnReason, ReturnReasonName = t.ReturnReason?.ToString(),
        OriginalTransferId = t.OriginalTransferId,
        Note = t.Note, ConfirmedAt = t.ConfirmedAt, CreatedAt = t.CreatedAt,
        TotalAmount = t.Items.Sum(i => i.Quantity * i.UnitPrice),
        Items = t.Items.Select(i => new TransferItemDto
        {
            Id = i.Id, ProductId = i.ProductId, ProductName = i.Product.Name,
            UnitShortName = i.Product.Unit.ShortName,
            BatchId = i.BatchId, LotNumber = i.Batch?.LotNumber,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice,
            TotalPrice = i.Quantity * i.UnitPrice
        }).ToList()
    };
}
