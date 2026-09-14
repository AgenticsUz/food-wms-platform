using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Tests.Infrastructure;

/// <summary>
/// Test ma'lumotini YAGONA joyda yasaydi: ombor + joy, kategoriya + birlik bilan
/// mahsulot, partiya bilan qoldiq, kontragent, agent, xodim profili.
/// </summary>
/// <remarks>
/// Hamma yordamchi <see cref="WmsDbContext"/> ni oladi va o'zi saqlaydi — tenantni
/// <c>StampEntries</c> qo'yadi, ya'ni test <c>TenantId</c> ga hech qachon tegmaydi
/// (CLAUDE.md §3.1). Sana qiymatlari UTC: ustunlar <c>timestamptz</c>.
/// </remarks>
public static class TestData
{
    /// <summary>Xodim profili (<c>CreatedByUserId</c> uchun).</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="fullName">Ism.</param>
    /// <returns>Profil.</returns>
    public static async Task<UserProfile> AddUserAsync(WmsDbContext db, string fullName = "Test Xodim")
    {
        ArgumentNullException.ThrowIfNull(db);

        UserProfile profile = new()
        {
            IdentitySub = Guid.CreateVersion7(),
            FullName = fullName,
            IsActive = true,
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    /// <summary>Ombor va uning sukut joyi (qoldiq qatori joysiz bo'lolmaydi).</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="name">Nom.</param>
    /// <param name="type">Turi.</param>
    /// <returns>Ombor (<c>Locations</c> to'ldirilgan).</returns>
    public static async Task<Warehouse> AddWarehouseAsync(
        WmsDbContext db, string name = "Ombor", WarehouseType type = WarehouseType.Finished)
    {
        ArgumentNullException.ThrowIfNull(db);

        Warehouse warehouse = new() { Name = name, Type = type };
        Location location = new() { Warehouse = warehouse, Name = "Default", Code = "DEF" };

        db.Warehouses.Add(warehouse);
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        warehouse.Locations.Add(location);
        return warehouse;
    }

    /// <summary>Mahsulot; kategoriya va birlik yo'q bo'lsa yasaladi.</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="name">Nom.</param>
    /// <param name="shelfLifeDays">Yaroqlilik muddati (kun) — kirimda partiya muddatini beradi.</param>
    /// <param name="costPrice">Tannarx.</param>
    /// <param name="barcode">Shtrix-kod.</param>
    /// <param name="type">Turi.</param>
    /// <returns>Mahsulot.</returns>
    public static async Task<Product> AddProductAsync(
        WmsDbContext db,
        string name = "Mahsulot",
        int? shelfLifeDays = null,
        decimal? costPrice = null,
        string? barcode = null,
        ProductType type = ProductType.Finished)
    {
        ArgumentNullException.ThrowIfNull(db);

        Category category = await db.Categories.FirstOrDefaultAsync()
            ?? await AddCategoryAsync(db);
        Unit unit = await db.Units.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Birliklar yo'q — TenantBaseline yurgizilmaganmi?");

        Product product = new()
        {
            Name = name,
            CategoryId = category.Id,
            UnitId = unit.Id,
            Type = type,
            ShelfLifeDays = shelfLifeDays,
            CostPrice = costPrice,
            Barcode = barcode,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    /// <summary>Kategoriya.</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="name">Nom.</param>
    /// <returns>Kategoriya.</returns>
    public static async Task<Category> AddCategoryAsync(WmsDbContext db, string name = "Umumiy")
    {
        ArgumentNullException.ThrowIfNull(db);

        Category category = new() { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    /// <summary>Partiya va unga bog'langan qoldiq qatori.</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="warehouse">Ombor (sukut joyi bo'lishi shart).</param>
    /// <param name="product">Mahsulot.</param>
    /// <param name="quantity">Miqdor.</param>
    /// <param name="expiry">Yaroqlilik sanasi (FEFO tartibi shunga qarab).</param>
    /// <param name="reserved">Band qilingan miqdor.</param>
    /// <param name="lotNumber">Partiya raqami.</param>
    /// <returns>Yaratilgan partiya va qoldiq.</returns>
    public static async Task<(Batch Batch, WarehouseStock Stock)> AddStockAsync(
        WmsDbContext db,
        Warehouse warehouse,
        Product product,
        decimal quantity,
        DateTime? expiry = null,
        decimal reserved = 0m,
        string? lotNumber = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(product);

        Guid locationId = warehouse.Locations.Count > 0
            ? warehouse.Locations.First().Id
            : await db.Locations.Where(l => l.WarehouseId == warehouse.Id).Select(l => l.Id).FirstAsync();

        Batch batch = new()
        {
            ProductId = product.Id,
            LotNumber = lotNumber ?? $"LOT-{Guid.NewGuid().ToString("N")[..8]}",
            ManufacturedDate = DateTime.UtcNow.Date,
            ExpiryDate = expiry,
            InitialQuantity = quantity,
            RemainingQuantity = quantity,
        };

        WarehouseStock stock = new()
        {
            WarehouseId = warehouse.Id,
            LocationId = locationId,
            ProductId = product.Id,
            Batch = batch,
            Quantity = quantity,
            ReservedQuantity = reserved,
        };

        db.Batches.Add(batch);
        db.WarehouseStocks.Add(stock);
        await db.SaveChangesAsync();
        return (batch, stock);
    }

    /// <summary>Kontragent.</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="name">Nom.</param>
    /// <param name="type">Turi.</param>
    /// <returns>Kontragent.</returns>
    public static async Task<Counterparty> AddCounterpartyAsync(
        WmsDbContext db, string name = "Mijoz", CounterpartyType type = CounterpartyType.Client)
    {
        ArgumentNullException.ThrowIfNull(db);

        Counterparty counterparty = new() { Name = name, Type = type };
        db.Counterparties.Add(counterparty);
        await db.SaveChangesAsync();
        return counterparty;
    }

    /// <summary>Agent (komissiya testlari uchun).</summary>
    /// <param name="db">Kontekst.</param>
    /// <param name="name">Nom.</param>
    /// <param name="commissionPercent">Sukut komissiya foizi.</param>
    /// <returns>Agent.</returns>
    public static async Task<Agent> AddAgentAsync(
        WmsDbContext db, string name = "Agent", decimal commissionPercent = 10m)
    {
        ArgumentNullException.ThrowIfNull(db);

        Agent agent = new() { Name = name, CommissionPercent = commissionPercent, IsActive = true };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();
        return agent;
    }
}
