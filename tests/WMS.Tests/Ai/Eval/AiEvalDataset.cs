using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Application.Common;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Ai.Eval;

/// <summary>
/// Sinov to'plami uchun MA'LUM ma'lumot: har savolning javobi oldindan bilinadi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Nomlar ATAYLAB shunday tanlangan: «Plombir» uchta mahsulotga, «Korzinka» ikkita
/// kontragentga to'g'ri keladi — noaniqlik stsenariylari (F10 §0.7) shu yerda o'lchanadi.
/// «Snikers» esa bitta: kirill/lotin yozuvi tekshiriladigan yagona nom.
/// </para>
/// <para>
/// Sonlar YAXLIT va bir-biridan farqli: javobda «34» chiqsa, u aynan Snikers ekani
/// shubhasiz bo'lsin. Ikki mahsulotda bir xil qoldiq bo'lsa, model noto'g'ri tool
/// chaqirib ham to'g'ri son aytishi mumkin edi va test buni sezmasdi.
/// </para>
/// </remarks>
internal sealed class AiEvalDataset
{
    private AiEvalDataset(
        Warehouse mainWarehouse,
        Warehouse finishedWarehouse,
        Counterparty korzinka,
        Counterparty makro,
        Counterparty supplier,
        Product snikers)
    {
        MainWarehouse = mainWarehouse;
        FinishedWarehouse = finishedWarehouse;
        Korzinka = korzinka;
        Makro = makro;
        Supplier = supplier;
        Snikers = snikers;
    }

    public Warehouse MainWarehouse { get; }

    public Warehouse FinishedWarehouse { get; }

    public Counterparty Korzinka { get; }

    public Counterparty Makro { get; }

    public Counterparty Supplier { get; }

    public Product Snikers { get; }

    /// <summary>Tenantni sinov ma'lumoti bilan to'ldiradi.</summary>
    /// <param name="scope">Tenant qamrovi (AI yoqilgan bo'lishi kerak).</param>
    /// <param name="userId">Hujjat yaratuvchi profil.</param>
    /// <returns>Yaratilgan yozuvlar.</returns>
    public static async Task<AiEvalDataset> SeedAsync(WmsTenantScope scope, Guid userId)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        Warehouse main = await TestData.AddWarehouseAsync(scope.Db, "Asosiy sklad");
        Warehouse finished = await TestData.AddWarehouseAsync(scope.Db, "Tayyor mahsulot", WarehouseType.Finished);
        await Searchable(scope, main, finished);

        Product snikers = await AiTestSupport.AddSearchableProductAsync(scope, "Snikers");
        Product plombirVanil = await AiTestSupport.AddSearchableProductAsync(scope, "Plombir vanilli");
        Product plombirShokolad = await AiTestSupport.AddSearchableProductAsync(scope, "Plombir shokoladli");
        Product plombirKlubnika = await AiTestSupport.AddSearchableProductAsync(scope, "Plombir klubnikali");
        Product shakar = await AiTestSupport.AddSearchableProductAsync(scope, "Shakar");
        Product sut = await AiTestSupport.AddSearchableProductAsync(scope, "Sut");
        Product kefir = await AiTestSupport.AddSearchableProductAsync(scope, "Kefir");
        Product tvorog = await AiTestSupport.AddSearchableProductAsync(scope, "Tvorog");

        // ⚠️ «Un» ATAYLAB qoldiqsiz: «yo'q mahsulot» va «nol qoldiq» boshqa-boshqa javob
        // va model ikkalasini bir xil aytmasligi kerak.
        await AiTestSupport.AddSearchableProductAsync(scope, "Un");

        // Qadoq (P2.7) va tannarx — A3 qoralamalari uchun: "50 quti" va kirim narxi
        // aynan shu ikki maydondan hisoblanadi.
        snikers.PackSize = 12m;
        snikers.PackUnit = "quti";
        shakar.CostPrice = 7_000m;
        await scope.Db.SaveChangesAsync(ct);

        DateTime today = DateTime.UtcNow.Date;

        // ⚠️ 38, 34 emas: quyida 4 dona TASDIQLANGAN sotuv bor va qoldiq undan keyin 34
        // bo'ladi. Sonni «kutilgan javob»ga qarab emas, HAYOTIY ketma-ketlikka qarab
        // qo'yish kerak — aks holda to'plam o'z ma'lumotiga mos kelmasdi.
        await TestData.AddStockAsync(scope.Db, main, snikers, 38);
        await TestData.AddStockAsync(scope.Db, main, plombirVanil, 120);
        await TestData.AddStockAsync(scope.Db, main, plombirShokolad, 80);
        await TestData.AddStockAsync(scope.Db, main, plombirKlubnika, 60);
        await TestData.AddStockAsync(scope.Db, main, shakar, 2500);

        // Muddat stsenariylari: 5 kun qoldi, 2 kun qoldi, 3 kun oldin tugagan.
        await TestData.AddStockAsync(scope.Db, main, sut, 300, today.AddDays(5));
        await TestData.AddStockAsync(scope.Db, main, kefir, 150, today.AddDays(2));
        await TestData.AddStockAsync(scope.Db, main, tvorog, 40, today.AddDays(-3));

        Counterparty korzinka = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");
        Counterparty korzinkaServis = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka Servis");
        Counterparty makro = await TestData.AddCounterpartyAsync(scope.Db, "Makro");

        // Nomi NOYOB mijoz: qoralama stsenariylari noaniqlikka urilib qolmasin
        // ("Korzinka" ataylab ikkita va u faqat savol berish uchun ishlatiladi).
        Counterparty oltinVodiy = await TestData.AddCounterpartyAsync(scope.Db, "Oltin Vodiy");
        Counterparty supplier = await TestData.AddCounterpartyAsync(scope.Db, "Baraka Taminot", CounterpartyType.Supplier);
        await Searchable(scope, korzinka, korzinkaServis, makro, oltinVodiy, supplier);

        ITransferService transfers = scope.Service<ITransferService>();

        // Tasdiqlangan sotuv — `last_price` shundan narx oladi (12 000, Korzinka).
        TransferDto sale = await transfers.CreateAsync(userId, new CreateTransferDto
        {
            Type = TransferType.Outgoing,
            FromWarehouseId = main.Id,
            CounterpartyId = korzinka.Id,
            Items = [new CreateTransferItemDto { ProductId = snikers.Id, Quantity = 4, UnitPrice = 12_000m }],
        });
        await transfers.ConfirmAsync(sale.Id);

        // Tasdiq kutayotgan uchta hujjat (ikki chiqim, bir kirim).
        await transfers.CreateAsync(userId, new CreateTransferDto
        {
            Type = TransferType.Outgoing,
            FromWarehouseId = main.Id,
            CounterpartyId = makro.Id,
            Items = [new CreateTransferItemDto { ProductId = shakar.Id, Quantity = 100, UnitPrice = 9_000m }],
        });

        await transfers.CreateAsync(userId, new CreateTransferDto
        {
            Type = TransferType.Outgoing,
            FromWarehouseId = main.Id,
            CounterpartyId = korzinka.Id,
            Items = [new CreateTransferItemDto { ProductId = plombirVanil.Id, Quantity = 10, UnitPrice = 15_000m }],
        });

        await transfers.CreateAsync(userId, new CreateTransferDto
        {
            Type = TransferType.Incoming,
            ToWarehouseId = finished.Id,
            CounterpartyId = supplier.Id,
            Items = [new CreateTransferItemDto { ProductId = shakar.Id, Quantity = 500, UnitPrice = 7_000m }],
        });

        // ⚠️ Qarz balansi TRANSFERLARDAN KEYIN qo'yiladi: tasdiqlangan sotuv Korzinkaning
        // qarziga 48 000 qo'shadi va oldin yozilgan «yaxlit» qiymat jimgina siljib ketardi.
        // To'plamdagi kutilgan son bitta joyda — shu yerda — hal bo'lishi kerak.
        await SetDebtAsync(scope, korzinka.Id, 1_500_000m);
        await SetDebtAsync(scope, makro.Id, -400_000m);
        await SetDebtAsync(scope, oltinVodiy.Id, 5_400_000m);

        return new AiEvalDataset(main, finished, korzinka, makro, supplier, snikers);
    }

    /// <summary>Kontragentning qarz balansini aniq qiymatga qo'yadi.</summary>
    /// <remarks>
    /// Tasdiqlangan hujjat qatorni allaqachon yaratgan bo'lishi mumkin — shuning uchun
    /// «topilmasa yarat», har safar yangi qator emas (balans bo'yicha noyob).
    /// </remarks>
    private static async Task SetDebtAsync(WmsTenantScope scope, Guid counterpartyId, decimal amount)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        Debt? debt = await scope.Db.Debts.FirstOrDefaultAsync(d => d.CounterpartyId == counterpartyId, ct);
        if (debt is null)
        {
            debt = new Debt { CounterpartyId = counterpartyId };
            scope.Db.Debts.Add(debt);
        }

        debt.Amount = amount;
        await scope.Db.SaveChangesAsync(ct);
    }

    /// <summary>Qidiruv ustunini prod qoidasi bilan to'ldiradi.</summary>
    /// <remarks>Izohi <see cref="AiTestSupport.AddSearchableProductAsync"/> da — sabab bir xil.</remarks>
    private static async Task Searchable(WmsTenantScope scope, params Warehouse[] warehouses)
    {
        foreach (Warehouse warehouse in warehouses)
        {
            warehouse.NameSearch = SearchNormalizer.Normalize(warehouse.Name);
        }

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task Searchable(WmsTenantScope scope, params Counterparty[] counterparties)
    {
        foreach (Counterparty counterparty in counterparties)
        {
            counterparty.NameSearch = SearchNormalizer.Normalize(counterparty.Name);
        }

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
