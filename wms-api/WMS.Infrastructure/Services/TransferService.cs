using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

using WMS.Application.Common;

namespace WMS.Infrastructure.Services;

public class TransferService : ITransferService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;
    public TransferService(WmsDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task<List<TransferDto>> GetAllAsync(int tenantId, TransferType? type,
        TransferStatus? status, DateTime? from, DateTime? to, int? counterpartyId, int page, int pageSize)
    {
        var q = _db.Transfers.Where(t => t.TenantId == tenantId)
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Agent)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .AsQueryable();

        if (type.HasValue) q = q.Where(t => t.Type == type.Value);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (counterpartyId.HasValue) q = q.Where(t => t.CounterpartyId == counterpartyId.Value);
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
        await ValidateCreateAsync(tenantId, dto);

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
            throw new AppException("Only pending transfers can be confirmed");

        // Stock, status, debt and commission must change together or not at all.
        await using var tx = await _db.Database.BeginTransactionAsync();

        switch (transfer.Type)
        {
            case TransferType.Incoming:
                await AddStockAsync(transfer, tenantId, "LOT");
                break;
            case TransferType.Outgoing:
                await DeductStockAsync(transfer, tenantId, reduceBatchRemaining: true);
                break;
            case TransferType.Internal:
                await ProcessInternal(transfer, tenantId);
                break;
            case TransferType.Return:
                if (transfer.OriginalTransferId.HasValue)
                    await ValidateReturnAgainstOriginal(tenantId, transfer.OriginalTransferId.Value,
                        transfer.CounterpartyId, transfer.Items.Select(i => (i.ProductId, i.Quantity)).ToList());
                await AddStockAsync(transfer, tenantId, "LOT-RET");
                break;
        }

        transfer.Status = TransferStatus.Confirmed;
        transfer.ConfirmedAt = DateTime.UtcNow;

        await UpdateDebt(transfer, tenantId);
        await CreateCommission(transfer, tenantId);
        await AdjustCommissionForReturn(transfer, tenantId);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // Notifications are best-effort and happen after the business data is committed.
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
            throw new AppException("Only pending transfers can be rejected");
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
            ?? throw new NotFoundException("Transfer not found");
        if (transfer.Status != TransferStatus.Pending)
            throw new AppException("Only pending transfers can be cancelled");
        transfer.Status = TransferStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    // ── Validation ──

    private async Task ValidateCreateAsync(int tenantId, CreateTransferDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new AppException("Transfer must contain at least one item");
        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new AppException("Item quantity must be greater than zero");
        if (dto.Items.Any(i => i.UnitPrice < 0))
            throw new AppException("Item price cannot be negative");
        if (dto.CommissionPercent is < 0 or > 100)
            throw new AppException("Commission percent must be between 0 and 100");

        switch (dto.Type)
        {
            case TransferType.Incoming when dto.ToWarehouseId == null:
                throw new AppException("Incoming transfer must have a destination warehouse");
            case TransferType.Outgoing when dto.FromWarehouseId == null:
                throw new AppException("Outgoing transfer must have a source warehouse");
            case TransferType.Internal when dto.FromWarehouseId == null || dto.ToWarehouseId == null:
                throw new AppException("Internal transfer must have both source and destination warehouses");
            case TransferType.Internal when dto.FromWarehouseId == dto.ToWarehouseId:
                throw new AppException("Internal transfer source and destination must differ");
            case TransferType.Return when dto.ToWarehouseId == null:
                throw new AppException("Return transfer must have a destination warehouse");
        }

        if (dto.OriginalTransferId.HasValue && dto.Type != TransferType.Return)
            throw new AppException("OriginalTransferId is only valid for return transfers");

        // Every referenced entity must belong to the calling tenant.
        var warehouseIds = new[] { dto.FromWarehouseId, dto.ToWarehouseId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (warehouseIds.Count > 0)
        {
            var found = await _db.Warehouses
                .CountAsync(w => w.TenantId == tenantId && warehouseIds.Contains(w.Id));
            if (found != warehouseIds.Count) throw new NotFoundException("Warehouse not found");
        }

        if (dto.CounterpartyId.HasValue &&
            !await _db.Counterparties.AnyAsync(c => c.Id == dto.CounterpartyId.Value && c.TenantId == tenantId))
            throw new NotFoundException("Counterparty not found");

        if (dto.AgentId.HasValue &&
            !await _db.Agents.AnyAsync(a => a.Id == dto.AgentId.Value && a.TenantId == tenantId))
            throw new NotFoundException("Agent not found");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var productCount = await _db.Products
            .CountAsync(p => p.TenantId == tenantId && productIds.Contains(p.Id));
        if (productCount != productIds.Count) throw new NotFoundException("Product not found");

        var batchIds = dto.Items.Where(i => i.BatchId.HasValue)
            .Select(i => i.BatchId!.Value).Distinct().ToList();
        if (batchIds.Count > 0)
        {
            var batchCount = await _db.Batches
                .CountAsync(b => b.TenantId == tenantId && batchIds.Contains(b.Id));
            if (batchCount != batchIds.Count) throw new NotFoundException("Batch not found");
        }

        if (dto.Type == TransferType.Return && dto.OriginalTransferId.HasValue)
            await ValidateReturnAgainstOriginal(tenantId, dto.OriginalTransferId.Value,
                dto.CounterpartyId, dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList());
    }

    private async Task ValidateReturnAgainstOriginal(int tenantId, int originalTransferId,
        int? counterpartyId, List<(int ProductId, decimal Quantity)> items)
    {
        var original = await _db.Transfers.Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == originalTransferId && t.TenantId == tenantId)
            ?? throw new NotFoundException("Original transfer not found");
        if (original.Type != TransferType.Outgoing || original.Status != TransferStatus.Confirmed)
            throw new AppException("Original transfer must be a confirmed outgoing sale");
        if (counterpartyId != original.CounterpartyId)
            throw new AppException("Return counterparty must match the original sale");

        // Decimal aggregates are not translatable on SQLite — sum client-side.
        var previouslyReturned = await _db.Transfers
            .Where(t => t.TenantId == tenantId && t.Type == TransferType.Return
                && t.OriginalTransferId == originalTransferId && t.Status == TransferStatus.Confirmed)
            .SelectMany(t => t.Items.Select(i => new { i.ProductId, i.Quantity }))
            .ToListAsync();

        foreach (var group in items.GroupBy(i => i.ProductId))
        {
            var sold = original.Items.Where(i => i.ProductId == group.Key).Sum(i => i.Quantity);
            var returned = previouslyReturned.Where(i => i.ProductId == group.Key).Sum(i => i.Quantity);
            var returning = group.Sum(i => i.Quantity);
            if (sold <= 0)
                throw new AppException($"Product {group.Key} was not part of the original sale");
            if (returning > sold - returned)
                throw new AppException(
                    $"Return quantity for product {group.Key} exceeds the remaining sold quantity ({sold - returned:N2})");
        }
    }

    // ── Stock movements ──

    /// Adds stock for Incoming and Return transfers. For returns linked to an original sale
    /// the new batch keeps the original manufacture/expiry dates so FEFO stays honest.
    private async Task AddStockAsync(Transfer transfer, int tenantId, string lotPrefix)
    {
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new AppException($"{transfer.Type} transfer must have a destination warehouse");

        var location = await GetOrCreateDefaultLocation(toWarehouseId);

        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        Dictionary<int, Batch>? originalBatches = null;
        if (transfer.Type == TransferType.Return && transfer.OriginalTransferId.HasValue)
        {
            originalBatches = (await _db.TransferItems
                    .Include(i => i.Batch)
                    .Where(i => i.TransferId == transfer.OriginalTransferId.Value && i.BatchId != null)
                    .ToListAsync())
                .Where(i => i.Batch != null) // soft-deleted batches come back as null navs
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.First().Batch!);
        }

        var now = DateTime.UtcNow;
        foreach (var item in transfer.Items)
        {
            products.TryGetValue(item.ProductId, out var product);
            var originalBatch = originalBatches?.GetValueOrDefault(item.ProductId);

            var batch = new Batch
            {
                TenantId = tenantId, ProductId = item.ProductId,
                LotNumber = $"{lotPrefix}-{now:yyyyMMdd}-{item.ProductId}-{Guid.NewGuid().ToString()[..6]}",
                ManufacturedDate = originalBatch?.ManufacturedDate ?? now,
                ExpiryDate = originalBatch != null
                    ? originalBatch.ExpiryDate
                    : product?.ShelfLifeDays != null ? now.AddDays(product.ShelfLifeDays.Value) : null,
                InitialQuantity = item.Quantity, RemainingQuantity = item.Quantity
            };
            _db.Batches.Add(batch);
            item.Batch = batch;

            _db.WarehouseStocks.Add(new WarehouseStock
            {
                TenantId = tenantId, WarehouseId = toWarehouseId,
                Location = location, ProductId = item.ProductId,
                Batch = batch, Quantity = item.Quantity
            });
        }
    }

    /// FEFO-deducts each item from the source warehouse and returns what was actually taken
    /// from which stock row/batch. Reserved stock is never touched.
    private async Task<List<(TransferItem Item, WarehouseStock Stock, decimal Qty)>> DeductStockAsync(
        Transfer transfer, int tenantId, bool reduceBatchRemaining)
    {
        var fromWarehouseId = transfer.FromWarehouseId
            ?? throw new AppException($"{transfer.Type} transfer must have a source warehouse");

        var deductions = new List<(TransferItem, WarehouseStock, decimal)>();

        foreach (var item in transfer.Items)
        {
            var remaining = item.Quantity;

            // FEFO: earliest expiry first. Availability (Quantity - Reserved) is decimal
            // arithmetic, which SQLite cannot compare server-side — filter in memory.
            var stocks = (await _db.WarehouseStocks
                    .Include(s => s.Batch)
                    .Where(s => s.TenantId == tenantId && s.WarehouseId == fromWarehouseId
                        && s.ProductId == item.ProductId && s.Quantity > 0)
                    .OrderBy(s => s.Batch.ExpiryDate ?? DateTime.MaxValue)
                    .ToListAsync())
                .Where(s => s.Quantity - s.ReservedQuantity > 0)
                .ToList();

            foreach (var stock in stocks)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, stock.Quantity - stock.ReservedQuantity);
                stock.Quantity -= take;
                if (reduceBatchRemaining)
                    stock.Batch.RemainingQuantity -= take;
                remaining -= take;
                deductions.Add((item, stock, take));
            }

            if (remaining > 0)
                throw new AppException($"Insufficient available stock for product {item.ProductId}");
        }

        return deductions;
    }

    private async Task ProcessInternal(Transfer transfer, int tenantId)
    {
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new AppException("Internal transfer must have a destination warehouse");
        if (transfer.FromWarehouseId == toWarehouseId)
            throw new AppException("Internal transfer source and destination must differ");

        // Goods stay within the company: batch remaining quantity is unchanged,
        // and the destination is credited with exactly the batches FEFO deducted.
        var deductions = await DeductStockAsync(transfer, tenantId, reduceBatchRemaining: false);
        var location = await GetOrCreateDefaultLocation(toWarehouseId);

        foreach (var group in deductions.GroupBy(d => new { d.Item.ProductId, d.Stock.BatchId }))
        {
            var qty = group.Sum(d => d.Qty);
            var dest = await _db.WarehouseStocks.FirstOrDefaultAsync(s =>
                s.TenantId == tenantId && s.WarehouseId == toWarehouseId
                && s.ProductId == group.Key.ProductId && s.BatchId == group.Key.BatchId);

            if (dest != null)
                dest.Quantity += qty;
            else
                _db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId, WarehouseId = toWarehouseId,
                    Location = location, ProductId = group.Key.ProductId,
                    BatchId = group.Key.BatchId, Quantity = qty
                });
        }
    }

    private async Task<Location> GetOrCreateDefaultLocation(int warehouseId)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == warehouseId);
        if (location == null)
        {
            location = new Location { WarehouseId = warehouseId, Name = "Default", Code = "DEF" };
            _db.Locations.Add(location);
        }
        return location;
    }

    // ── Debt & commission ──

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
        var percent = Math.Clamp(transfer.CommissionPercent ?? agent.CommissionPercent, 0m, 100m);
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

    /// A return reduces the original sale's commission proportionally to the returned value.
    /// If the commission was already paid out, a negative clawback record is created instead
    /// so the paid history stays intact and the agent's due balance absorbs the difference.
    private async Task AdjustCommissionForReturn(Transfer transfer, int tenantId)
    {
        if (transfer.Type != TransferType.Return || transfer.OriginalTransferId == null) return;

        var commission = await _db.CommissionRecords.FirstOrDefaultAsync(c =>
            c.TenantId == tenantId
            && c.TransferId == transfer.OriginalTransferId.Value
            && c.Status != CommissionStatus.Cancelled);
        if (commission == null) return;

        var returnAmount = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);
        var reduction = Math.Min(commission.CommissionAmount,
            Math.Round(returnAmount * commission.CommissionPercent / 100m, 2));
        if (reduction <= 0) return;

        if (commission.IsPaid)
        {
            _db.CommissionRecords.Add(new CommissionRecord
            {
                TenantId = tenantId,
                AgentId = commission.AgentId,
                TransferId = transfer.Id,
                SaleAmount = -returnAmount,
                CommissionPercent = commission.CommissionPercent,
                CommissionAmount = -reduction,
                Status = CommissionStatus.Confirmed
            });
        }
        else
        {
            commission.SaleAmount -= returnAmount;
            commission.CommissionAmount -= reduction;
            if (commission.CommissionAmount <= 0)
                commission.Status = CommissionStatus.Cancelled;
        }
    }

    // ── Notifications ──

    private async Task CheckLowStock(Transfer transfer, int tenantId)
    {
        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _db.Products.Include(p => p.Unit)
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id) && p.MinStock > 0)
            .ToListAsync();
        if (products.Count == 0) return;

        var stocks = (await _db.WarehouseStocks
                .Where(s => s.TenantId == tenantId && productIds.Contains(s.ProductId))
                .Select(s => new { s.ProductId, s.Quantity })
                .ToListAsync())
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));

        foreach (var product in products)
        {
            var currentStock = stocks.GetValueOrDefault(product.Id, 0m);
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
            ?? throw new NotFoundException("Transfer not found");
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
