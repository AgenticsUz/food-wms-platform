using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Category : TenantEntity
{
    public string Name { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class Unit : TenantEntity
{
    public string Name { get; set; } = null!;
    public string ShortName { get; set; } = null!;
}

public class Product : TenantEntity
{
    public string Name { get; set; } = null!;

    /// <summary>
    /// <see cref="Name"/> ning qidiruv shakli (P2.1) — GIN trigram indeks shu ustunda.
    /// </summary>
    /// <remarks>
    /// ⚠️ Hisoblanadigan ustun EMAS: kirill→lotin o'girish C# da
    /// (<c>SearchNormalizer.Normalize</c>), shuning uchun nomni o'zgartirgan HAR joy
    /// (servis, import, seed) buni ham yangilaydi.
    /// </remarks>
    public string NameSearch { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public ProductType Type { get; set; }
    public decimal MinStock { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }

    /// <summary>Bitta qadoqdagi asosiy birlik miqdori (1 quti = N dona). <see langword="null"/> — qadoq yo'q.</summary>
    /// <remarks>
    /// ⚠️ Qoldiq, FEFO va hisobotlar DOIM asosiy birlikda (<see cref="UnitId"/>) yuritiladi —
    /// qadoq faqat KIRITISH qulayligi: «50 quti» → 50 × <see cref="PackSize"/> dona.
    /// Ikkinchi o'lchov birligini bazaga kiritish (qoldiqni quti va donada saqlash)
    /// ATAYLAB qilinmadi: o'sha yo'l har hisobotda «qaysi birlikda?» savolini tug'diradi.
    /// </remarks>
    public decimal? PackSize { get; set; }

    /// <summary>Qadoq nomi («quti», «karobka», «paket») — faqat ko'rsatish uchun.</summary>
    public string? PackUnit { get; set; }
}
