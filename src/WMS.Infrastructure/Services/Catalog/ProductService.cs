using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

public class ProductService : IProductService
{
    /// <summary>Qidiruvda bir so'rovda qaytariladigan eng ko'p qator.</summary>
    /// <remarks>
    /// Qidiruv natijasi «eng yaqin nomzodlar» — undan kattasi foydalanuvchiga ham,
    /// AI qatlamiga ham ma'nosiz (REJA: ko'p bo'lsa «toraytiring» deyiladi).
    /// </remarks>
    private const int SearchPageLimit = 100;

    private readonly WmsDbContext _db;
    private readonly ISearchService _search;
    public ProductService(WmsDbContext db, ISearchService search)
    { _db = db; _search = search; }

    public async Task<List<ProductDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            return await SearchAsync(search, page, pageSize);
        }

        return await _db.Products
            // Kalit ikkinchi tartib: StampEntries bitta SaveChanges'dagi hamma yozuvga BIR XIL
            // CreatedAt qo'yadi (Excel importi), faqat vaqt bo'yicha sahifalash qatorlarni
            // sahifalar orasida aralashtirib yuborardi.
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
    }

    /// <summary>
    /// Nom bo'yicha qidiruv: nomzodlarni <c>ISearchService</c> beradi, DTO shakli esa
    /// oddiy ro'yxatniki bilan AYNAN bir xil qoladi (frontend javobni farqlamaydi).
    /// </summary>
    private async Task<List<ProductDto>> SearchAsync(string search, int page, int pageSize)
    {
        int take = Math.Clamp(pageSize, 1, SearchPageLimit);
        int skip = Math.Max(page - 1, 0) * take;

        // Sahifalash nomzodlar RO'YXATI ustida: `skip + take` tasini so'rab, keraklisini kesamiz.
        IReadOnlyList<ProductSearchCandidate> candidates = await _search.FindProductsAsync(search, skip + take);
        List<Guid> ids = candidates.Skip(skip).Select(c => c.Id).ToList();
        if (ids.Count == 0) return [];

        List<ProductDto> rows = await _db.Products.Where(p => ids.Contains(p.Id)).Select(ToDto).ToListAsync();

        // `IN (...)` tartibni SAQLAMAYDI — bal bo'yicha tartibni qaytarib qo'yamiz.
        Dictionary<Guid, int> rank = ids.Index().ToDictionary(x => x.Item, x => x.Index);
        return rows.OrderBy(r => rank[r.Id]).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id)
    {
        return await _db.Products.Where(p => p.Id == id).Select(ToDto).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Product not found");
    }

    public async Task<ProductDto> GetByBarcodeAsync(string barcode)
    {
        // Skaner kod oxiriga bo'sh joy/`\r` qo'shishi mumkin; bo'sh kod — topilmadi (hamma
        // shtrix-kodsiz mahsulotni qaytarib yubormasin).
        var code = barcode?.Trim();
        if (string.IsNullOrEmpty(code))
            throw new NotFoundException("Product not found");

        // Shtrix-kod noyob emas (indeks faqat qidiruv uchun) — bir nechta bo'lsa tartib barqaror.
        return await _db.Products
            .Where(p => p.Barcode == code)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Select(ToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Product not found");
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        await ValidateCategoryAndUnitAsync(dto.CategoryId, dto.UnitId);
        (decimal? packSize, string? packUnit) = NormalizePack(dto.PackSize, dto.PackUnit);

        var product = new Product
        {
            // Qidiruv ustuni nom bilan BIRGA yoziladi (P2.1): hisoblanadigan ustun bo'lolmaydi,
            // chunki kirill→lotin o'girish C# da.
            Name = dto.Name, NameSearch = SearchNormalizer.Normalize(dto.Name), CategoryId = dto.CategoryId,
            UnitId = dto.UnitId, Type = dto.Type, MinStock = dto.MinStock,
            ShelfLifeDays = dto.ShelfLifeDays, Barcode = dto.Barcode, CostPrice = dto.CostPrice,
            PackSize = packSize, PackUnit = packUnit
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var p = await _db.Products.Include(x => x.Category).Include(x => x.Unit)
            .FirstAsync(x => x.Id == product.Id);
        return MapToDto(p);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Product not found");

        await ValidateCategoryAndUnitAsync(dto.CategoryId, dto.UnitId);
        (decimal? packSize, string? packUnit) = NormalizePack(dto.PackSize, dto.PackUnit);

        p.Name = dto.Name; p.NameSearch = SearchNormalizer.Normalize(dto.Name);
        p.CategoryId = dto.CategoryId; p.UnitId = dto.UnitId;
        p.Type = dto.Type; p.MinStock = dto.MinStock; p.ShelfLifeDays = dto.ShelfLifeDays;
        p.Barcode = dto.Barcode; p.CostPrice = dto.CostPrice;
        p.PackSize = packSize; p.PackUnit = packUnit;
        await _db.SaveChangesAsync();

        await _db.Entry(p).Reference(x => x.Category).LoadAsync();
        await _db.Entry(p).Reference(x => x.Unit).LoadAsync();
        return MapToDto(p);
    }

    public async Task DeleteAsync(Guid id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Product not found");
        p.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var all = await _db.Categories.AsNoTracking().ToListAsync();
        return BuildCategoryTree(all, null);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (dto.ParentId.HasValue &&
            !await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value))
            throw new NotFoundException("Parent category not found");

        var cat = new Category { Name = dto.Name, ParentId = dto.ParentId };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = cat.Id, Name = cat.Name, ParentId = cat.ParentId };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Category not found");

        if (dto.ParentId == id)
            throw new AppException("A category cannot be its own parent");
        if (dto.ParentId.HasValue &&
            !await _db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value))
            throw new NotFoundException("Parent category not found");

        cat.Name = dto.Name;
        cat.ParentId = dto.ParentId;
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = cat.Id, Name = cat.Name, ParentId = cat.ParentId };
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Category not found");
        cat.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<UnitDto>> GetUnitsAsync()
    {
        return await _db.Units
            .Select(u => new UnitDto { Id = u.Id, Name = u.Name, ShortName = u.ShortName })
            .ToListAsync();
    }

    public async Task<UnitDto> CreateUnitAsync(CreateUnitDto dto)
    {
        var unit = new Unit { Name = dto.Name, ShortName = dto.ShortName };
        _db.Units.Add(unit);
        await _db.SaveChangesAsync();
        return new UnitDto { Id = unit.Id, Name = unit.Name, ShortName = unit.ShortName };
    }

    public async Task<UnitDto> UpdateUnitAsync(Guid id, UpdateUnitDto dto)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("Unit not found");
        unit.Name = dto.Name;
        unit.ShortName = dto.ShortName;
        await _db.SaveChangesAsync();
        return new UnitDto { Id = unit.Id, Name = unit.Name, ShortName = unit.ShortName };
    }

    public async Task DeleteUnitAsync(Guid id)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("Unit not found");
        unit.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private async Task ValidateCategoryAndUnitAsync(Guid categoryId, Guid unitId)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId);
        if (!categoryExists) throw new NotFoundException("Category not found");
        var unitExists = await _db.Units.AnyAsync(u => u.Id == unitId);
        if (!unitExists) throw new NotFoundException("Unit not found");
    }

    /// SQL proyeksiyasi (ro'yxat, id va shtrix-kod bo'yicha) — bitta shakl, uch joyda.
    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductDto>> ToDto = p => new ProductDto
    {
        Id = p.Id, Name = p.Name, CategoryId = p.CategoryId,
        CategoryName = p.Category.Name, UnitId = p.UnitId,
        UnitName = p.Unit.Name, UnitShortName = p.Unit.ShortName,
        Type = p.Type, MinStock = p.MinStock, ShelfLifeDays = p.ShelfLifeDays,
        Barcode = p.Barcode, CostPrice = p.CostPrice,
        PackSize = p.PackSize, PackUnit = p.PackUnit
    };

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, CategoryId = p.CategoryId,
        CategoryName = p.Category.Name, UnitId = p.UnitId,
        UnitName = p.Unit.Name, UnitShortName = p.Unit.ShortName,
        Type = p.Type, MinStock = p.MinStock, ShelfLifeDays = p.ShelfLifeDays,
        Barcode = p.Barcode, CostPrice = p.CostPrice,
        PackSize = p.PackSize, PackUnit = p.PackUnit
    };

    /// <summary>
    /// Qadoq maydonlarini tekshiradi va tozalaydi: <c>PackSize</c> va <c>PackUnit</c> — JUFT.
    /// </summary>
    /// <remarks>
    /// ⚠️ Konversiya (quti → dona) bu yerda ATAYLAB yo'q: qoldiq, FEFO va hisobotlar doim
    /// asosiy birlikda yuritiladi, qadoq esa faqat kiritish qulayligi (formada o'giriladi).
    /// Yarim to'ldirilgan juftlik («50» — nimaning ellikta?) shu sababdan xato.
    /// </remarks>
    private static (decimal? PackSize, string? PackUnit) NormalizePack(decimal? packSize, string? packUnit)
    {
        string? unit = string.IsNullOrWhiteSpace(packUnit) ? null : packUnit.Trim();

        if (packSize is { } size && size <= 0)
            throw new AppException("Pack size must be greater than zero");
        if (packSize is not null && unit is null)
            throw new AppException("Pack unit is required when pack size is set");
        if (packSize is null && unit is not null)
            throw new AppException("Pack size is required when pack unit is set");

        return (packSize, unit);
    }

    private static List<CategoryDto> BuildCategoryTree(List<Category> all, Guid? parentId)
    {
        return all.Where(c => c.ParentId == parentId).Select(c => new CategoryDto
        {
            Id = c.Id, Name = c.Name, ParentId = c.ParentId,
            Children = BuildCategoryTree(all, c.Id)
        }).ToList();
    }
}
