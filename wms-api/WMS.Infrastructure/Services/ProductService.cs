using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Products;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly WmsDbContext _db;
    public ProductService(WmsDbContext db) => _db = db;

    public async Task<List<ProductDto>> GetAllAsync(int tenantId, int page = 1, int pageSize = 20)
    {
        return await _db.Products
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Category).Include(p => p.Unit)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, CategoryId = p.CategoryId,
                CategoryName = p.Category.Name, UnitId = p.UnitId,
                UnitName = p.Unit.Name, UnitShortName = p.Unit.ShortName,
                Type = p.Type, MinStock = p.MinStock, ShelfLifeDays = p.ShelfLifeDays,
                Barcode = p.Barcode, CostPrice = p.CostPrice
            }).ToListAsync();
    }

    public async Task<ProductDto> CreateAsync(int tenantId, CreateProductDto dto)
    {
        await ValidateCategoryAndUnitAsync(tenantId, dto.CategoryId, dto.UnitId);

        var product = new Product
        {
            TenantId = tenantId, Name = dto.Name, CategoryId = dto.CategoryId,
            UnitId = dto.UnitId, Type = dto.Type, MinStock = dto.MinStock,
            ShelfLifeDays = dto.ShelfLifeDays, Barcode = dto.Barcode, CostPrice = dto.CostPrice
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var p = await _db.Products.Include(x => x.Category).Include(x => x.Unit)
            .FirstAsync(x => x.Id == product.Id);
        return MapToDto(p);
    }

    public async Task<ProductDto> UpdateAsync(int tenantId, int id, UpdateProductDto dto)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Product not found");

        await ValidateCategoryAndUnitAsync(tenantId, dto.CategoryId, dto.UnitId);

        p.Name = dto.Name; p.CategoryId = dto.CategoryId; p.UnitId = dto.UnitId;
        p.Type = dto.Type; p.MinStock = dto.MinStock; p.ShelfLifeDays = dto.ShelfLifeDays;
        p.Barcode = dto.Barcode; p.CostPrice = dto.CostPrice;
        await _db.SaveChangesAsync();

        await _db.Entry(p).Reference(x => x.Category).LoadAsync();
        await _db.Entry(p).Reference(x => x.Unit).LoadAsync();
        return MapToDto(p);
    }

    public async Task DeleteAsync(int tenantId, int id)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Product not found");
        p.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(int tenantId)
    {
        var all = await _db.Categories.Where(c => c.TenantId == tenantId).ToListAsync();
        return BuildCategoryTree(all, null);
    }

    public async Task<CategoryDto> CreateCategoryAsync(int tenantId, CreateCategoryDto dto)
    {
        var cat = new Category { TenantId = tenantId, Name = dto.Name, ParentId = dto.ParentId };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = cat.Id, Name = cat.Name, ParentId = cat.ParentId };
    }

    public async Task<CategoryDto> UpdateCategoryAsync(int tenantId, int id, UpdateCategoryDto dto)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
            ?? throw new NotFoundException("Category not found");
        cat.Name = dto.Name;
        cat.ParentId = dto.ParentId;
        await _db.SaveChangesAsync();
        return new CategoryDto { Id = cat.Id, Name = cat.Name, ParentId = cat.ParentId };
    }

    public async Task DeleteCategoryAsync(int tenantId, int id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId)
            ?? throw new NotFoundException("Category not found");
        cat.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<UnitDto>> GetUnitsAsync(int tenantId)
    {
        return await _db.Units.Where(u => u.TenantId == tenantId)
            .Select(u => new UnitDto { Id = u.Id, Name = u.Name, ShortName = u.ShortName })
            .ToListAsync();
    }

    public async Task<UnitDto> CreateUnitAsync(int tenantId, CreateUnitDto dto)
    {
        var unit = new Unit { TenantId = tenantId, Name = dto.Name, ShortName = dto.ShortName };
        _db.Units.Add(unit);
        await _db.SaveChangesAsync();
        return new UnitDto { Id = unit.Id, Name = unit.Name, ShortName = unit.ShortName };
    }

    public async Task<UnitDto> UpdateUnitAsync(int tenantId, int id, UpdateUnitDto dto)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new NotFoundException("Unit not found");
        unit.Name = dto.Name;
        unit.ShortName = dto.ShortName;
        await _db.SaveChangesAsync();
        return new UnitDto { Id = unit.Id, Name = unit.Name, ShortName = unit.ShortName };
    }

    public async Task DeleteUnitAsync(int tenantId, int id)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId)
            ?? throw new NotFoundException("Unit not found");
        unit.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private async Task ValidateCategoryAndUnitAsync(int tenantId, int categoryId, int unitId)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId && c.TenantId == tenantId);
        if (!categoryExists) throw new NotFoundException("Category not found");
        var unitExists = await _db.Units.AnyAsync(u => u.Id == unitId && u.TenantId == tenantId);
        if (!unitExists) throw new NotFoundException("Unit not found");
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id, Name = p.Name, CategoryId = p.CategoryId,
        CategoryName = p.Category.Name, UnitId = p.UnitId,
        UnitName = p.Unit.Name, UnitShortName = p.Unit.ShortName,
        Type = p.Type, MinStock = p.MinStock, ShelfLifeDays = p.ShelfLifeDays,
        Barcode = p.Barcode, CostPrice = p.CostPrice
    };

    private static List<CategoryDto> BuildCategoryTree(List<Category> all, int? parentId)
    {
        return all.Where(c => c.ParentId == parentId).Select(c => new CategoryDto
        {
            Id = c.Id, Name = c.Name, ParentId = c.ParentId,
            Children = BuildCategoryTree(all, c.Id)
        }).ToList();
    }
}
