using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Trade;

public class TransferService : ITransferService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ITenantStateService _tenantState;
    private readonly IRequestWarnings _warnings;
    private readonly SubscriptionOptions _subscription;
    private readonly ILogger<TransferService> _logger;
    private readonly IStockAllocator _stock;

    private readonly ITelegramPartnerNotifier _partners;

    public TransferService(WmsDbContext db, INotificationService notifications,
        ITenantStateService tenantState, IRequestWarnings warnings,
        IOptions<SubscriptionOptions> subscription, ILogger<TransferService> logger,
        IStockAllocator stock, ITelegramPartnerNotifier partners)
    {
        _db = db; _notifications = notifications; _tenantState = tenantState;
        _warnings = warnings; _subscription = subscription.Value; _logger = logger;
        _stock = stock; _partners = partners;
    }

    public async Task<List<TransferDto>> GetAllAsync(TransferType? type,
        TransferStatus? status, DateTime? from, DateTime? to, Guid? counterpartyId, int page, int pageSize)
    {
        var q = _db.Transfers.AsNoTracking()
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

        // Manfiy Skip Postgres'da 500 berardi — noto'g'ri sahifa raqami mijoz xatosi, server emas.
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        var transfers = await q.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return transfers.Select(t => MapToDto(t)).ToList();
    }

    public async Task<TransferDto> GetByIdAsync(Guid id)
    {
        var t = await GetTransferEntity(id, tracking: false);
        return MapToDto(t);
    }

    public async Task<TransferDto> CreateAsync(Guid userId, CreateTransferDto dto)
    {
        await PlanLimits.EnsureCanCreateTransferAsync(_db);
        await EnsureTransferTypeAllowedAsync(dto.Type);
        await ValidateCreateAsync(dto);

        var transfer = new Transfer
        {
            Type = dto.Type, FromWarehouseId = dto.FromWarehouseId,
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

        await PlanLimits.ReportUsageAsync(_db, _warnings, PlanLimits.TransfersThisMonth, _subscription.LimitWarnPercent, _notifications);

        // Tasdiqlovchilarga (transfers.confirm) — Telegram'da «Tasdiqlash/Rad etish» tugmalari bilan (TG9).
        // Ilgari yaratilganda hech kim xabar olmasdi — menejer ilovani ochmaguncha «kutilmoqda» turardi.
        await NotifySafelyAsync(transfer.Id, () => NotifyPendingAsync(transfer.Id, userId));

        return await GetByIdAsync(transfer.Id);
    }

    private async Task NotifyPendingAsync(Guid transferId, Guid createdByUserId)
    {
        var t = await _db.Transfers.AsNoTracking()
            .Include(x => x.Counterparty).Include(x => x.FromWarehouse).Include(x => x.ToWarehouse).Include(x => x.Items)
            .FirstAsync(x => x.Id == transferId);
        string creator = await _db.UserProfiles.AsNoTracking().Where(u => u.Id == createdByUserId).Select(u => u.FullName).FirstOrDefaultAsync() ?? "—";
        string amount = NotificationMessages.Amount(t.Items.Sum(i => i.Quantity * i.UnitPrice));
        string date = NotificationMessages.Date(t.CreatedAt);
        string party = t.Counterparty?.Name ?? t.FromWarehouse?.Name ?? t.ToWarehouse?.Name ?? "—";

        (string template, string?[] args) = t.Type switch
        {
            TransferType.Return => (NotificationMessages.ReturnPending, new string?[] { party, date, amount, creator }),
            TransferType.Incoming => (NotificationMessages.IncomingPending, new string?[] { party, date, amount, creator }),
            TransferType.Internal => (NotificationMessages.InternalPending, new string?[] { t.FromWarehouse?.Name ?? "—", t.ToWarehouse?.Name ?? "—", date, creator }),
            _ => (NotificationMessages.SalePending, new string?[] { party, date, amount, creator }),
        };

        await _notifications.NotifyAsync(null, NotificationMessages.TransferPendingTitle, template, args,
            NotificationType.TransferPending, "Transfer", t.Id);
    }

    /// <summary>
    /// Tasdiq: zaxira, partiya qoldig'i, holat, qarz va komissiya — BITTA ish birligida.
    /// </summary>
    /// <remarks>
    /// D13: SQLite davrida parallel ikki tasdiq ikkalasi ham «Pending» ni ko'rib, zaxirani ikki
    /// marta ayirardi. Endi transfer, zaxira, partiya va qarz qatorlari <c>xmin</c> bilan
    /// qo'riqlanadi va hammasi bitta <c>SaveChangesAsync</c> da yoziladi: ikkinchi tasdiqning
    /// <c>UPDATE ... WHERE xmin = @eski</c> si 0 qator oladi → 409, tranzaksiya to'liq qaytadi —
    /// yarim qo'llangan zaxira yoki qarz qolmaydi.
    /// </remarks>
    public async Task<TransferDto> ConfirmAsync(Guid id)
    {
        var transfer = await GetTransferEntity(id, tracking: true);
        if (transfer.Status != TransferStatus.Pending)
            throw new AppException("Only pending transfers can be confirmed");

        // Qarz qatori tranzaksiyadan OLDIN kafolatlanadi — sababi DebtLedger izohida
        // (noyoblik to'qnashuvini ochiq tranzaksiya ichida yutib bo'lmaydi).
        var debt = transfer.CounterpartyId is { } counterpartyId && ChangesDebt(transfer.Type)
            ? await DebtLedger.GetOrCreateAsync(_db, counterpartyId)
            : null;

        // Stock, status, debt and commission must change together or not at all.
        await using (var tx = await _db.Database.BeginTransactionAsync())
        {
            switch (transfer.Type)
            {
                case TransferType.Incoming:
                    await AddStockAsync(transfer, "LOT");
                    break;
                case TransferType.Outgoing:
                    await DeductStockAsync(transfer, reduceBatchRemaining: true);
                    break;
                case TransferType.Internal:
                    await ProcessInternal(transfer);
                    break;
                case TransferType.Return:
                    if (transfer.OriginalTransferId.HasValue)
                    {
                        var original = await ValidateReturnAgainstOriginal(transfer.OriginalTransferId.Value,
                            transfer.CounterpartyId, transfer.Items.Select(i => (i.ProductId, i.Quantity)).ToList());

                        // Asl sotuv qatoriga «tegamiz» — uning xmin'i ish birligiga kiradi. Aks holda bitta
                        // sotuvga qarshi parallel ikki qaytarish har biri «avval qaytarilgan» miqdorni
                        // eski holatda ko'rib, sotilgandan ko'pini qaytarib yuborardi.
                        original.UpdatedAt = DateTime.UtcNow;
                    }
                    await AddStockAsync(transfer, "LOT-RET");
                    break;
            }

            transfer.Status = TransferStatus.Confirmed;
            transfer.ConfirmedAt = DateTime.UtcNow;

            ApplyDebt(transfer, debt);
            await CreateCommission(transfer);
            await AdjustCommissionForReturn(transfer);

            await SaveUnitOfWorkAsync();
            await tx.CommitAsync();
        }

        // Notifications are best-effort and happen after the business data is committed.
        if (transfer.Type == TransferType.Outgoing || transfer.Type == TransferType.Internal)
            await NotifySafelyAsync(transfer.Id, () => CheckLowStock(transfer));

        // Guid o'rniga odam o'qiydigan belgi: kontragent/ombor va sana (TG4). Qisqa raqam
        // qarori (HOLAT «keyinga qolgan») chiqsa shu yerda almashadi.
        string amount = NotificationMessages.Amount(transfer.Items.Sum(i => i.Quantity * i.UnitPrice));
        string date = NotificationMessages.Date(transfer.CreatedAt);
        string party = transfer.Counterparty?.Name ?? transfer.FromWarehouse?.Name ?? transfer.ToWarehouse?.Name ?? "—";

        (string title, string template, string?[] args, NotificationType type) = transfer.Type switch
        {
            TransferType.Return => (NotificationMessages.ReturnReceivedTitle, NotificationMessages.ReturnReceived,
                new string?[] { party, date, amount }, NotificationType.ReturnReceived),
            TransferType.Incoming => (NotificationMessages.TransferConfirmedTitle, NotificationMessages.IncomingConfirmed,
                new string?[] { party, date, amount }, NotificationType.TransferConfirmed),
            TransferType.Internal => (NotificationMessages.TransferConfirmedTitle, NotificationMessages.InternalConfirmed,
                new string?[] { transfer.FromWarehouse?.Name ?? "—", transfer.ToWarehouse?.Name ?? "—", date }, NotificationType.TransferConfirmed),
            _ => (NotificationMessages.TransferConfirmedTitle, NotificationMessages.SaleConfirmed,
                new string?[] { party, date, amount }, NotificationType.TransferConfirmed),
        };

        await NotifySafelyAsync(transfer.Id, () => _notifications.NotifyAsync(null, title, template, args, type, "Transfer", transfer.Id));

        // Mijozga (TG13): sotuv tasdiqlandi — tenant ruxsat bergan va kontragent ulangan bo'lsa.
        if (transfer.Type == TransferType.Outgoing && transfer.CounterpartyId is { } clientId)
            await NotifySafelyAsync(transfer.Id, () => _partners.NotifyClientAsync(clientId, NotificationMessages.ClientOrderConfirmed,
                [date, amount], $"client:confirmed:{transfer.Id:N}"));

        return MapToDto(transfer);
    }

    public async Task<TransferDto> RejectAsync(Guid id)
    {
        var transfer = await GetTransferEntity(id, tracking: true);
        if (transfer.Status != TransferStatus.Pending)
            throw new AppException("Only pending transfers can be rejected");
        transfer.Status = TransferStatus.Rejected;

        // xmin: parallel tasdiq bilan to'qnashsa bittasi 409 oladi (tasdiqlangan transfer rad etilmaydi).
        await _db.SaveChangesAsync();

        await NotifySafelyAsync(transfer.Id, () => _notifications.NotifyAsync(null,
            NotificationMessages.TransferRejectedTitle, NotificationMessages.TransferRejected,
            [transfer.Counterparty?.Name ?? transfer.FromWarehouse?.Name ?? transfer.ToWarehouse?.Name ?? "—", NotificationMessages.Date(transfer.CreatedAt)],
            NotificationType.TransferRejected, "Transfer", transfer.Id));

        return MapToDto(transfer);
    }

    public async Task CancelAsync(Guid id)
    {
        var transfer = await _db.Transfers.FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new NotFoundException("Transfer not found");
        if (transfer.Status != TransferStatus.Pending)
            throw new AppException("Only pending transfers can be cancelled");
        transfer.Status = TransferStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    // ── Validation ──

    /// <summary>
    /// Incoming, outgoing, internal and return movements are sold separately: a customer
    /// that only receives goods should not be paying for the sales side. The check lives
    /// here rather than in an attribute because the entitlement depends on the payload.
    /// </summary>
    private async Task EnsureTransferTypeAllowedAsync(TransferType type)
    {
        var code = type switch
        {
            TransferType.Incoming => FeatureCodes.TransfersIncoming,
            TransferType.Outgoing => FeatureCodes.TransfersOutgoing,
            TransferType.Internal => FeatureCodes.TransfersInternal,
            TransferType.Return => FeatureCodes.TransfersReturn,
            _ => null   // ProductionOutput is created by the system, never by a user request
        };
        if (code == null || _db.CurrentTenantId is not { } tenantId) return;

        var state = await _tenantState.GetAsync(tenantId);
        if (state != null && !state.EnabledFeatures.Contains(code))
            throw new FeatureDisabledException(code);
    }

    private async Task ValidateCreateAsync(CreateTransferDto dto)
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

        // Havola qilingan har yozuv joriy tenantniki bo'lsin — filtr + RLS begona qatorni
        // ko'rsatmaydi, ya'ni sanoq mos kelmasa u yo yo'q, yo boshqa tenantniki.
        var warehouseIds = new[] { dto.FromWarehouseId, dto.ToWarehouseId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (warehouseIds.Count > 0)
        {
            var found = await _db.Warehouses.CountAsync(w => warehouseIds.Contains(w.Id));
            if (found != warehouseIds.Count) throw new NotFoundException("Warehouse not found");
        }

        if (dto.CounterpartyId.HasValue &&
            !await _db.Counterparties.AnyAsync(c => c.Id == dto.CounterpartyId.Value))
            throw new NotFoundException("Counterparty not found");

        if (dto.AgentId.HasValue &&
            !await _db.Agents.AnyAsync(a => a.Id == dto.AgentId.Value))
            throw new NotFoundException("Agent not found");

        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var productCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id));
        if (productCount != productIds.Count) throw new NotFoundException("Product not found");

        var batchIds = dto.Items.Where(i => i.BatchId.HasValue)
            .Select(i => i.BatchId!.Value).Distinct().ToList();
        if (batchIds.Count > 0)
        {
            var batchCount = await _db.Batches.CountAsync(b => batchIds.Contains(b.Id));
            if (batchCount != batchIds.Count) throw new NotFoundException("Batch not found");
        }

        if (dto.Type == TransferType.Return && dto.OriginalTransferId.HasValue)
            await ValidateReturnAgainstOriginal(dto.OriginalTransferId.Value,
                dto.CounterpartyId, dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList());

        await EnsureStockAvailableAsync(dto);
    }

    /// <summary>
    /// Omborga chiqim beradigan hujjatda zaxira YARATISHDA tekshiriladi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu KAFOLAT emas, ERTA XABAR: hujjat yaratilgandan tasdiqlangangacha boshqa
    /// hujjat o'sha qoldiqni olib ketishi mumkin, shuning uchun tasdiqdagi tekshiruv
    /// (FEFO yechuvchining `Shortfall` i) JOYIDA QOLADI. Ilgari bu yerda tekshiruv umuman
    /// yo'q edi: menejer hujjatni yaratib, mijozga aytib bo'lgach, tasdiqda «qoldiq yo'q»
    /// xabarini olardi va sabab qayerdaligini bilmasdi.
    /// </remarks>
    private async Task EnsureStockAvailableAsync(CreateTransferDto dto)
    {
        // Kirim, qaytarish va ishlab chiqarish chiqimi omborga QO'SHADI — tekshirmaymiz.
        if (dto.Type is not (TransferType.Outgoing or TransferType.Internal)
            || dto.FromWarehouseId is not { } fromWarehouseId)
        {
            return;
        }

        // Bir mahsulot bir necha qatorda kelishi mumkin — talab qatorlar bo'yicha yig'iladi.
        Dictionary<Guid, decimal> required = dto.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        Dictionary<Guid, decimal> available =
            await _stock.GetAvailableAsync(required.Keys, fromWarehouseId);

        List<Guid> shortIds = [.. required.Where(r => available.GetValueOrDefault(r.Key) < r.Value).Select(r => r.Key)];
        if (shortIds.Count == 0)
        {
            return;
        }

        // Nom xabar uchun: birinchi yetmagan mahsulot — menejer bitta aniq qatorni tuzatadi.
        Dictionary<Guid, string> names = await _db.Products.AsNoTracking()
            .Where(p => shortIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        Guid productId = shortIds[0];
        throw new AppException("Insufficient available stock for product {0}: need {1:N2}, available {2:N2}",
            names.GetValueOrDefault(productId) ?? productId.ToString(),
            required[productId],
            available.GetValueOrDefault(productId));
    }

    /// <returns>Asl sotuv (kuzatiladi) — tasdiq uni ish birligiga qo'shadi.</returns>
    private async Task<Transfer> ValidateReturnAgainstOriginal(Guid originalTransferId,
        Guid? counterpartyId, List<(Guid ProductId, decimal Quantity)> items)
    {
        var original = await _db.Transfers.Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == originalTransferId)
            ?? throw new NotFoundException("Original transfer not found");
        if (original.Type != TransferType.Outgoing || original.Status != TransferStatus.Confirmed)
            throw new AppException("Original transfer must be a confirmed outgoing sale");
        if (counterpartyId != original.CounterpartyId)
            throw new AppException("Return counterparty must match the original sale");

        // Oldin qaytarilgan miqdor SQL'da yig'iladi (SQLite davrida xotirada edi).
        var previouslyReturned = await _db.Transfers
            .Where(t => t.Type == TransferType.Return
                && t.OriginalTransferId == originalTransferId && t.Status == TransferStatus.Confirmed)
            .SelectMany(t => t.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        foreach (var group in items.GroupBy(i => i.ProductId))
        {
            var sold = original.Items.Where(i => i.ProductId == group.Key).Sum(i => i.Quantity);
            var returned = previouslyReturned.GetValueOrDefault(group.Key);
            var returning = group.Sum(i => i.Quantity);
            if (sold <= 0)
                throw new AppException("Product {0} was not part of the original sale", group.Key);
            if (returning > sold - returned)
                throw new AppException(
                    "Return quantity for product {0} exceeds the remaining sold quantity ({1:N2})",
                    group.Key, sold - returned);
        }

        return original;
    }

    // ── Stock movements ──

    /// Adds stock for Incoming and Return transfers. For returns linked to an original sale
    /// the new batch keeps the original manufacture/expiry dates so FEFO stays honest.
    private async Task AddStockAsync(Transfer transfer, string lotPrefix)
    {
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new AppException("{0} transfer must have a destination warehouse", transfer.Type);

        var location = await GetOrCreateDefaultLocation(toWarehouseId);

        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        Dictionary<Guid, Batch>? originalBatches = null;
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
                ProductId = item.ProductId,
                // Guid mahsulot kaliti 36 belgi — lot raqamiga uning tasodifiy DUMI (v7 boshi vaqt,
                // bir paytda yaratilgan mahsulotlarda bir xil) kiradi; noyoblikni oxirgi qism beradi.
                LotNumber = $"{lotPrefix}-{now:yyyyMMdd}-{item.ProductId.ToString("N")[^6..]}-{Guid.NewGuid().ToString("N")[..6]}",
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
                WarehouseId = toWarehouseId,
                Location = location, ProductId = item.ProductId,
                Batch = batch, Quantity = item.Quantity
            });
        }
    }

    /// FEFO-deducts each item from the source warehouse and returns what was actually taken
    /// from which stock row/batch. Reserved stock is never touched.
    private async Task<List<(TransferItem Item, WarehouseStock Stock, decimal Qty)>> DeductStockAsync(
        Transfer transfer, bool reduceBatchRemaining)
    {
        var fromWarehouseId = transfer.FromWarehouseId
            ?? throw new AppException("{0} transfer must have a source warehouse", transfer.Type);

        var deductions = new List<(TransferItem, WarehouseStock, decimal)>();

        foreach (var item in transfer.Items)
        {
            // FEFO tartibi, «zaxiradagiga tegilmaydi» qoidasi va SQL filtri — ombor modulining
            // umumiy yordamchisida (ishlab chiqarish bilan bitta nusxa). U SaveChanges chaqirmaydi:
            // o'zgarishlar tasdiqning yagona ish birligiga kiradi (xmin → 409).
            var allocation = await _stock.DeductFefoAsync(item.ProductId, item.Quantity,
                fromWarehouseId, reduceBatchRemaining);

            if (!allocation.IsSatisfied)
                throw new AppException("Insufficient available stock for product {0}: need {1:N2}, available {2:N2}",
                    item.Product?.Name ?? item.ProductId.ToString(),
                    item.Quantity,
                    item.Quantity - allocation.Shortfall);

            deductions.AddRange(allocation.Lines.Select(l => (item, l.Stock, l.Quantity)));
        }

        return deductions;
    }

    private async Task ProcessInternal(Transfer transfer)
    {
        var toWarehouseId = transfer.ToWarehouseId
            ?? throw new AppException("Internal transfer must have a destination warehouse");
        if (transfer.FromWarehouseId == toWarehouseId)
            throw new AppException("Internal transfer source and destination must differ");

        // Goods stay within the company: batch remaining quantity is unchanged,
        // and the destination is credited with exactly the batches FEFO deducted.
        var deductions = await DeductStockAsync(transfer, reduceBatchRemaining: false);
        var location = await GetOrCreateDefaultLocation(toWarehouseId);

        foreach (var group in deductions.GroupBy(d => new { d.Item.ProductId, d.Stock.BatchId }))
        {
            var qty = group.Sum(d => d.Qty);
            var dest = await _db.WarehouseStocks.FirstOrDefaultAsync(s =>
                s.WarehouseId == toWarehouseId
                && s.ProductId == group.Key.ProductId && s.BatchId == group.Key.BatchId);

            if (dest != null)
                dest.Quantity += qty;
            else
                _db.WarehouseStocks.Add(new WarehouseStock
                {
                    WarehouseId = toWarehouseId,
                    Location = location, ProductId = group.Key.ProductId,
                    BatchId = group.Key.BatchId, Quantity = qty
                });
        }
    }

    private async Task<Location> GetOrCreateDefaultLocation(Guid warehouseId)
    {
        var location = await _db.Locations
            .Where(l => l.WarehouseId == warehouseId)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync();
        if (location == null)
        {
            location = new Location { WarehouseId = warehouseId, Name = "Default", Code = "DEF" };
            _db.Locations.Add(location);
        }
        return location;
    }

    // ── Debt & commission ──

    private static bool ChangesDebt(TransferType type) =>
        type is TransferType.Outgoing or TransferType.Incoming or TransferType.Return;

    private static void ApplyDebt(Transfer transfer, Debt? debt)
    {
        if (debt == null) return;

        // Yig'indi — shu transferning o'z qatorlari ustida (ular allaqachon yuklangan), jadval ustida emas.
        var totalPrice = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);

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

    private async Task CreateCommission(Transfer transfer)
    {
        // Only outgoing sales routed through an agent earn a commission.
        if (transfer.Type != TransferType.Outgoing || transfer.AgentId == null) return;

        // Avoid duplicates if confirm is somehow re-run. Parallel tasdiqni endi transferning xmin'i
        // va (tenant, transfer, agent) noyob indeksi to'sadi (D13) — bu tekshiruv birinchi to'r.
        var exists = await _db.CommissionRecords.AnyAsync(c => c.TransferId == transfer.Id);
        if (exists) return;

        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == transfer.AgentId.Value);
        if (agent == null) return;

        var saleAmount = transfer.Items.Sum(i => i.Quantity * i.UnitPrice);
        var percent = Math.Clamp(transfer.CommissionPercent ?? agent.CommissionPercent, 0m, 100m);
        var commission = Math.Round(saleAmount * percent / 100m, 2);

        _db.CommissionRecords.Add(new CommissionRecord
        {
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
    private async Task AdjustCommissionForReturn(Transfer transfer)
    {
        if (transfer.Type != TransferType.Return || transfer.OriginalTransferId == null) return;

        var commission = await _db.CommissionRecords.FirstOrDefaultAsync(c =>
            c.TransferId == transfer.OriginalTransferId.Value
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

    /// <summary>
    /// Tasdiqning yagona <c>SaveChangesAsync</c> i. Noyoblik buzilishi ham 409 ga aylanadi.
    /// </summary>
    /// <remarks>
    /// Parallel ikki tasdiqda EF bayonotlar tartibini kafolatlamaydi: ikkinchisi transferning
    /// <c>xmin</c> to'qnashuvidan OLDIN komissiya yoki zaxira qatorining noyob indeksiga urilishi
    /// mumkin (23505). Ma'nosi bir xil — «boshqa so'rov ulgurdi» — shuning uchun javob ham bir xil:
    /// 409, 500 emas. Tranzaksiya baribir to'liq qaytadi.
    /// </remarks>
    private async Task SaveUnitOfWorkAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && DebtLedger.IsUniqueViolation(ex))
        {
            throw new DbUpdateConcurrencyException(ex.Message, ex);
        }
    }

    // ── Notifications ──

    /// <summary>
    /// Bildirishnoma — qulaylik: biznes ma'lumoti allaqachon commit bo'lgan. Uning xatosi so'rovni
    /// yiqitsa, mijoz «tasdiq o'tmadi» deb qayta bosardi va «Only pending...» xatosini olardi.
    /// </summary>
    private async Task NotifySafelyAsync(Guid transferId, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Transfer {TransferId} bo'yicha bildirishnoma yuborilmadi", transferId);
        }
    }

    private async Task CheckLowStock(Transfer transfer)
    {
        var productIds = transfer.Items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _db.Products.AsNoTracking().Include(p => p.Unit)
            .Where(p => productIds.Contains(p.Id) && p.MinStock > 0)
            .ToListAsync();
        if (products.Count == 0) return;

        var stocks = await _db.WarehouseStocks
            .Where(s => productIds.Contains(s.ProductId))
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        foreach (var product in products)
        {
            var currentStock = stocks.GetValueOrDefault(product.Id, 0m);
            if (currentStock <= product.MinStock)
            {
                var unitName = product.Unit?.ShortName ?? "";
                await _notifications.NotifyAsync(null,
                    NotificationMessages.LowStockTitle, NotificationMessages.LowStock,
                    [product.Name, NotificationMessages.Quantity(currentStock), unitName, NotificationMessages.Quantity(product.MinStock)],
                    NotificationType.LowStock, "Product", product.Id);
            }
        }
    }

    private async Task<Transfer> GetTransferEntity(Guid id, bool tracking)
    {
        var q = _db.Transfers.AsQueryable();
        if (!tracking) q = q.AsNoTracking();

        return await q
            .Include(t => t.FromWarehouse).Include(t => t.ToWarehouse)
            .Include(t => t.Counterparty).Include(t => t.CreatedByUser)
            .Include(t => t.Agent)
            .Include(t => t.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
            .Include(t => t.Items).ThenInclude(i => i.Batch)
            .FirstOrDefaultAsync(t => t.Id == id)
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
