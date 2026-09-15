using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Warehouse : TenantEntity
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// <see cref="Name"/> ning qidiruv shakli (P2.1) — <c>Product.NameSearch</c> bilan bir xil qoida.
    /// </summary>
    public string NameSearch { get; set; } = string.Empty;

    public WarehouseType Type { get; set; }
    public string? Description { get; set; }
    public ICollection<Location> Locations { get; set; } = new List<Location>();
}

/// <summary>
/// Ombor ichidagi joy. ⚠️ SQLite davrida <c>tenant_id</c> yo'q edi (izolyatsiya
/// ombor orqali JOIN bilan); F6 da qo'shildi — RLS siyosati har jadvalda bir xil
/// shaklda bo'lsin (D4).
/// </summary>
public class Location : TenantEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
}

public class Batch : TenantEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LotNumber { get; set; } = null!;
    public DateTime ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    /// <summary>Shu partiyaning bir birlik TANNARXI (kirimdagi narx yoki ishlab chiqarish qiymati).</summary>
    /// <remarks>
    /// Nega partiyada, nega <c>Product.CostPrice</c> yetmaydi: bir mahsulot turli narxda
    /// kelaveradi va foyda AYNAN sotilgan partiyaning tannarxidan hisoblanadi. Mahsulot
    /// darajasidagi bitta raqam eski kirimlarni ham yangi narxga qayta baholab yuborardi.
    /// <see langword="null"/> — tannarx noma'lum (eski qatorlar, narxsiz kirim).
    /// </remarks>
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Zaxira qoldig'i (ombor + joy + mahsulot + partiya). Joyida o'zgartiriladigan
/// balans — shuning uchun optimistik versiya (<c>xmin</c>) bilan qo'riqlanadi (D13).
/// </summary>
public class WarehouseStock : TenantEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
}
