using Microsoft.EntityFrameworkCore;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

/// <summary>
/// <see cref="IStockAllocator"/> — FEFO yechishning yagona amalga oshirilishi.
/// </summary>
/// <remarks>
/// Eski <c>TransferService.DeductStockAsync</c> va <c>ProductionService.DeductFromStockAsync</c>
/// dan olingan (tartib va «zaxiradagiga tegilmaydi» qoidasi o'sha).
/// </remarks>
public sealed class StockAllocator : IStockAllocator
{
    private readonly WmsDbContext _db;

    public StockAllocator(WmsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Dictionary<Guid, decimal>> GetAvailableAsync(
        IReadOnlyCollection<Guid> productIds,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        // Guruhlash SQL'da: transferda 50 qator bo'lsa ham bitta so'rov.
        return await AvailableRows(warehouseId)
            .Where(s => productIds.Contains(s.ProductId))
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Available = g.Sum(s => s.Quantity - s.ReservedQuantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Available, cancellationToken);
    }

    /// <summary>
    /// Yechishga YAROQLI qatorlar — mavjudlik sharti, ombor filtri va partiya sharti.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yechish ham, «qancha bor» tekshiruvi ham SHU manbadan quriladi: ikki joyda ikki
    /// xil shart bo'lsa, tekshiruvdan o'tgan hujjat tasdiqda yiqilardi.
    /// </para>
    /// <para>
    /// ⚠️ Partiya sharti (<c>EXISTS</c>) ATAYLAB: FEFO tartibi partiya maydonlari bo'yicha
    /// va bog'lanish majburiy, ya'ni SQL'da INNER JOIN — yumshoq o'chirilgan partiyali
    /// qatorni yechish ko'rmaydi. Tekshiruv ham AYNAN shuni ko'rmasligi kerak, aks holda
    /// u «yetadi» der edi, tasdiq esa «yetmadi». ⚠️ Bunday qator qoldiq ekranida (partiyaga
    /// tegmaydigan <c>GetStockAsync</c>) KO'RINADI — «bor, lekin chiqmaydi» holati;
    /// `docs/XATOLAR-2026-09-14.md` §3 ga qarang.
    /// </para>
    /// </remarks>
    private IQueryable<WarehouseStock> AvailableRows(Guid? warehouseId)
    {
        IQueryable<WarehouseStock> query = _db.WarehouseStocks
            .Where(s => s.Quantity - s.ReservedQuantity > 0)
            .Where(s => _db.Batches.Any(b => b.Id == s.BatchId));

        return warehouseId is Guid id ? query.Where(s => s.WarehouseId == id) : query;
    }

    public async Task<FefoAllocation> DeductFefoAsync(
        Guid productId,
        decimal quantity,
        Guid? warehouseId = null,
        bool reduceBatchRemaining = true,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return new FefoAllocation([], 0);
        }

        // Mavjudlik sharti va FEFO tartibi SQL'da (SQLite davrida xotirada edi). Muddatsiz
        // partiya OXIRIDA: `expiry_date IS NULL` false < true. SQLite davridagi
        // `?? DateTime.MaxValue` Postgres'da timestamptz chegarasidan tashqari qiymat bo'lardi.
        // Bir xil muddatda — avval ishlab chiqarilgani, keyin kalit: tartib har safar bir xil bo'lsin.
        IOrderedQueryable<WarehouseStock> ordered = AvailableRows(warehouseId)
            .Include(s => s.Batch)
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.Batch.ExpiryDate == null)
            .ThenBy(s => s.Batch.ExpiryDate)
            .ThenBy(s => s.Batch.ManufacturedDate)
            .ThenBy(s => s.Id);

        List<(WarehouseStock Stock, decimal Take)> plan = [];
        decimal remaining = quantity;

        // Oqim bilan o'qiladi va kerakli miqdor yig'ilgach to'xtaydi — hamma partiya xotiraga
        // tortilmaydi.
        await foreach (WarehouseStock stock in ordered.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            // ⚠️ Mavjudlik KUZATUVDAGI qiymatdan qayta hisoblanadi: bir transferda bir mahsulot
            // ikki qatorda kelsa, birinchi qator shu so'rovda allaqachon kamaytirgan qatorni EF
            // xotiradagi nusxa bilan qaytaradi (SQL sharti bazadagi eski qiymatni ko'rgan).
            decimal available = stock.Quantity - stock.ReservedQuantity;
            if (available <= 0)
            {
                continue;
            }

            decimal take = Math.Min(remaining, available);
            plan.Add((stock, take));
            remaining -= take;

            if (remaining <= 0)
            {
                break;
            }
        }

        // Yetmasa hech narsa o'zgartirilmaydi: chaqiruvchi xato tashlaganda kuzatuvda yarim
        // kamaytirilgan qator qolmasin (so'rovning keyingi SaveChanges'i uni yozib yuborardi).
        if (remaining > 0)
        {
            return new FefoAllocation([], remaining);
        }

        List<FefoLine> lines = new(plan.Count);
        foreach ((WarehouseStock stock, decimal take) in plan)
        {
            stock.Quantity -= take;
            if (reduceBatchRemaining)
            {
                stock.Batch.RemainingQuantity -= take;
            }

            lines.Add(new FefoLine(stock, take));
        }

        return new FefoAllocation(lines, 0);
    }
}
