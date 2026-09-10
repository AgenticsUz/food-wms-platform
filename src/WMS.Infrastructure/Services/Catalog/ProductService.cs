using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Catalog;

public class ProductService : IProductService
{
    private readonly WmsDbContext _db;
    public ProductService(WmsDbContext db) => _db = db;

    public async Task<List<ProductDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _db.Products
            // Kalit ikkinchi tartib: StampEntries bitta SaveChanges'dagi hamma yozuvga BIR XIL
            // CreatedAt qo'yadi (Excel importi), faqat vaqt bo'yicha sahifalash qatorlarni
            // sahifalar orasida aralashtirib yuborardi.
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
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

        var product = new Product
        {
            Name = dto.Name, CategoryId = dto.CategoryId,
            UnitId = dto.UnitId, Type = dto.Type, MinStock = dto.MinStock,
            ShelfLifeDays = dto.ShelfLifeDays, Barcode = dto.Barcode, CostPrice = dto.CostPrice
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

        p.Name = dto.Name; p.CategoryId = dto.CategoryId; p.UnitId = dto.UnitId;
        p.Type = dto.Type; p.MinStock = dto.MinStock; p.ShelfLifeDays = dto.ShelfLifeDays;
        p.Barcode = dto.Barcode; p.CostPrice = dto.CostPrice;
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
        Barcode = p.Barcode, CostPrice = p.CostPrice
    };

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, CategoryId = p.CategoryId,
        CategoryName = p.Category.Name, UnitId = p.UnitId,
        UnitName = p.Unit.Name, UnitShortName = p.Unit.ShortName,
        Type = p.Type, MinStock = p.MinStock, ShelfLifeDays = p.ShelfLifeDays,
        Barcode = p.Barcode, CostPrice = p.CostPrice
    };

    private static List<CategoryDto> BuildCategoryTree(List<Category> all, Guid? parentId)
    {
        return all.Where(c => c.ParentId == parentId).Select(c => new CategoryDto
        {
            Id = c.Id, Name = c.Name, ParentId = c.ParentId,
            Children = BuildCategoryTree(all, c.Id)
        }).ToList();
    }
}
