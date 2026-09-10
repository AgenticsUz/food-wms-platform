using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Seeding;

// Katalog, omborlar va zaxira daftari (FEFO) — servislardagi qoidalar bilan bir xil.
internal sealed partial class DemoData
{
    private readonly Dictionary<Guid, Batch> _batches = [];
    private readonly List<WarehouseStock> _stocks = [];
    private int _lotSequence;

    private Warehouse _rawWarehouse = null!;
    private Warehouse _finishedWarehouse = null!;
    private Location _locA1 = null!;
    private Location _locA2 = null!;
    private Location _locB1 = null!;
    private Location _locB2 = null!;

    private Product _quruqSut = null!;
    private Product _quyuqSut = null!;
    private Product _shakar = null!;
    private Product _saryog = null!;
    private Product _kokos = null!;
    private Product _stabilizator = null!;
    private Product _plombir = null!;
    private Product _shokolad = null!;
    private Product _kremBrule = null!;

    private void BuildCatalog(Dictionary<string, Unit> units)
    {
        // Birliklarni TenantBaseline yozadi (eski demo'dagi oltitasi bilan bir xil). Tenant admini
        // birini o'chirgan bo'lsa — qayta qo'shiladi, aks holda mahsulot birliksiz qolardi.
        Unit EnsureUnit(string shortName, string name)
        {
            if (!units.TryGetValue(shortName, out Unit? unit))
            {
                unit = Add(new Unit { Name = name, ShortName = shortName });
                units[shortName] = unit;
            }

            _unitsById[unit.Id] = unit;
            return unit;
        }

        Unit kg = EnsureUnit("kg", "Kilogramm");
        Unit litr = EnsureUnit("litr", "Litr");
        Unit dona = EnsureUnit("dona", "Dona");

        Category xom = Add(new Category { Name = "Xom ashyo" });
        Category tayyor = Add(new Category { Name = "Tayyor mahsulot" });
        Add(new Category { Name = "Qadoqlash" });
        Category sut = Add(new Category { Name = "Sut mahsulotlari", ParentId = xom.Id });
        Category qand = Add(new Category { Name = "Qand va shakar", ParentId = xom.Id });
        Category yog = Add(new Category { Name = "Yog'lar", ParentId = xom.Id });
        Category muz = Add(new Category { Name = "Muzqaymoq", ParentId = tayyor.Id });
        Add(new Category { Name = "Qandolat", ParentId = tayyor.Id });

        // Saqlash muddati: eski demo partiyalarida xom ashyo +180 kun, tayyor mahsulot +365 kun edi;
        // endi u mahsulotda — servis ham partiya muddatini shundan hisoblaydi.
        Product Raw(string name, Category category, Unit unit, decimal minStock, decimal cost) =>
            AddProduct(new Product { Name = name, CategoryId = category.Id, UnitId = unit.Id, Type = ProductType.Raw, MinStock = minStock, CostPrice = cost, ShelfLifeDays = 180 });

        // Shtrix-kod — skaner oqimi (D1) demo'da bo'sh qolmasin; 478 — O'zbekiston GS1 prefiksi.
        Product Finished(string name, decimal minStock, decimal cost, string barcode) =>
            AddProduct(new Product { Name = name, CategoryId = muz.Id, UnitId = dona.Id, Type = ProductType.Finished, MinStock = minStock, CostPrice = cost, ShelfLifeDays = 365, Barcode = barcode });

        _quruqSut = Raw("Quruq sut", sut, kg, 100, 25_000);
        _quyuqSut = Raw("Quyuq sut", sut, litr, 50, 8_000);
        _shakar = Raw("Shakar", qand, kg, 200, 12_000);
        _saryog = Raw("Saryog'", yog, kg, 30, 45_000);
        _kokos = Raw("Kokos yog'i", yog, kg, 20, 38_000);
        _stabilizator = Raw("Stabilizator", xom, kg, 10, 95_000);

        _plombir = Finished("Plombir 100ml", 500, 3_500, "4780012340015");
        _shokolad = Finished("Shokoladli muzqaymoq", 300, 4_200, "4780012340022");
        _kremBrule = Finished("Krem-brule", 200, 3_800, "4780012340039");

        _rawWarehouse = Add(new Warehouse { Name = "Xom ashyo ombori", Type = WarehouseType.Raw, Description = "Sovutgichli xom ashyo ombori (+2…+6 °C)" });
        _finishedWarehouse = Add(new Warehouse { Name = "Tayyor mahsulot ombori", Type = WarehouseType.Finished, Description = "Muzlatgichli ombor (−20 °C)" });

        _locA1 = Add(new Location { WarehouseId = _rawWarehouse.Id, Name = "Sovutgich A-1", Code = "A1" });
        _locA2 = Add(new Location { WarehouseId = _rawWarehouse.Id, Name = "Sovutgich A-2", Code = "A2" });
        _locB1 = Add(new Location { WarehouseId = _finishedWarehouse.Id, Name = "Muzlatgich B-1", Code = "B1" });
        _locB2 = Add(new Location { WarehouseId = _finishedWarehouse.Id, Name = "Muzlatgich B-2", Code = "B2" });
    }

    private Product AddProduct(Product product)
    {
        Add(product);
        _productsById[product.Id] = product;
        return product;
    }

    /// <summary>Partiya — o'qiladigan, takrorlanmas lot raqami bilan (servisdagi Guid qo'shimchasi demo'da o'qib bo'lmasdi).</summary>
    private Batch NewBatch(Product product, string prefix, DateTime manufactured, DateTime? expiry, decimal quantity, DateTime at, string? notes = null)
    {
        _lotSequence++;
        Batch batch = Add(
            new Batch
            {
                ProductId = product.Id,
                LotNumber = $"{prefix}-{at.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)}-{_lotSequence:D3}",
                ManufacturedDate = manufactured,
                ExpiryDate = expiry,
                InitialQuantity = quantity,
                RemainingQuantity = quantity,
                Notes = notes,
            },
            at);

        _batches[batch.Id] = batch;
        return batch;
    }

    private static DateTime? ExpiryFrom(Product product, DateTime manufactured) =>
        product.ShelfLifeDays is { } days ? manufactured.AddDays(days) : null;

    private void AddStock(Warehouse warehouse, Location location, Product product, Batch batch, decimal quantity, DateTime at) =>
        _stocks.Add(Add(
            new WarehouseStock { WarehouseId = warehouse.Id, LocationId = location.Id, ProductId = product.Id, BatchId = batch.Id, Quantity = quantity },
            at));

    /// <summary>
    /// FEFO ayirish (<c>TransferService.DeductStockAsync</c> / <c>ProductionService.DeductFromStockAsync</c>
    /// qoidasi): eng erta tugaydigan partiyadan, band qilingan qismga tegmay; qoldiq ham, partiya
    /// qoldig'i ham kamayadi. <paramref name="from"/> = null — istalgan ombor (ishlab chiqarish).
    /// </summary>
    private List<(WarehouseStock Stock, decimal Quantity)> Take(Product product, decimal quantity, Warehouse? from)
    {
        List<(WarehouseStock, decimal)> taken = [];
        decimal remaining = quantity;

        // OrderBy barqaror: muddati teng partiyalarda avval kiritilgani olinadi.
        foreach (WarehouseStock stock in _stocks
                     .Where(s => s.ProductId == product.Id && (from is null || s.WarehouseId == from.Id) && s.Quantity - s.ReservedQuantity > 0)
                     .OrderBy(s => _batches[s.BatchId].ExpiryDate ?? DateTime.MaxValue)
                     .ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            decimal take = Math.Min(remaining, stock.Quantity - stock.ReservedQuantity);
            stock.Quantity -= take;
            _batches[stock.BatchId].RemainingQuantity -= take;
            remaining -= take;
            taken.Add((stock, take));
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException(
                $"Demo ssenariysi mos emas: '{product.Name}' uchun {Fmt(remaining)} yetishmadi — sanalar yoki miqdorlar buzilgan.");
        }

        return taken;
    }

    private decimal StockOf(Product product) => _stocks.Where(s => s.ProductId == product.Id).Sum(s => s.Quantity);

    /// <summary>
    /// Daftarni QAYTA hisoblab solishtiradi: partiya qoldig'i = uning zaxira qatorlari yig'indisi,
    /// hech narsa manfiy emas, har qarz = tasdiqlangan transferlar ± to'lovlar. Seed kodi keyin
    /// o'zgartirilsa nomuvofiq demo jim yozilmasin — tranzaksiya bekor bo'ladi.
    /// </summary>
    private void VerifyLedger()
    {
        foreach (Batch batch in _batches.Values)
        {
            decimal inStock = _stocks.Where(s => s.BatchId == batch.Id).Sum(s => s.Quantity);
            if (batch.RemainingQuantity != inStock || batch.RemainingQuantity < 0 || batch.RemainingQuantity > batch.InitialQuantity)
            {
                throw new InvalidOperationException(
                    $"Demo daftari: partiya {batch.LotNumber} qoldig'i {Fmt(batch.RemainingQuantity)}, zaxirada {Fmt(inStock)}.");
            }
        }

        Dictionary<Guid, decimal> expected = [];
        foreach (Transfer transfer in Created<Transfer>().Where(t => t.Status == TransferStatus.Confirmed && t.CounterpartyId is not null))
        {
            decimal sign = transfer.Type switch
            {
                TransferType.Outgoing => 1m,
                TransferType.Incoming or TransferType.Return => -1m,
                _ => 0m,
            };
            decimal total = _transferItems.Where(i => i.TransferId == transfer.Id).Sum(i => i.Quantity * i.UnitPrice);
            expected[transfer.CounterpartyId!.Value] = expected.GetValueOrDefault(transfer.CounterpartyId.Value) + (sign * total);
        }

        foreach ((PaymentHistory payment, PaymentDirection direction) in _payments)
        {
            decimal delta = direction == PaymentDirection.In ? -payment.Amount : payment.Amount;
            expected[payment.CounterpartyId] = expected.GetValueOrDefault(payment.CounterpartyId) + delta;
        }

        foreach (Debt debt in _debts.Values)
        {
            if (debt.Amount != expected.GetValueOrDefault(debt.CounterpartyId))
            {
                throw new InvalidOperationException(
                    $"Demo daftari: kontragent {debt.CounterpartyId} qarzi {Fmt(debt.Amount)}, harakatlardan {Fmt(expected.GetValueOrDefault(debt.CounterpartyId))}.");
            }
        }
    }
}
