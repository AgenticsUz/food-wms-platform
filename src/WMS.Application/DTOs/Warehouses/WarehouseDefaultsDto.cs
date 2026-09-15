namespace WMS.Application.DTOs.Warehouses;

/// <summary>
/// Standart ombor sozlamasi (P2.6): kirim/chiqim formasi omborni SO'RAMASLIGI uchun.
/// </summary>
/// <remarks>
/// <para>
/// Uch manba bor va ular shu tartibda ustun keladi: <see cref="UserWarehouseId"/> (xodimning
/// shaxsiy tanlovi) → tenant sozlamasi (<see cref="RawWarehouseId"/> /
/// <see cref="FinishedWarehouseId"/>) → tenantda ombor jami BITTA bo'lsa o'sha. Natija
/// <see cref="EffectiveRawId"/> va <see cref="EffectiveFinishedId"/> da — forma faqat shu
/// ikkisiga qaraydi, tartibni o'zi qayta hisoblamasin.
/// </para>
/// <para>
/// ⚠️ Yozishda (<c>PUT</c>) FAQAT <see cref="RawWarehouseId"/> va
/// <see cref="FinishedWarehouseId"/> o'qiladi: shaxsiy tanlov boshqa endpoint'da
/// (<c>PUT /api/settings/warehouses/mine</c>), chunki uning ruxsati ham boshqa —
/// o'z profilini har kim o'zgartira oladi.
/// </para>
/// <para>
/// ⚠️ O'chirilgan yoki boshqa tenantning ombori ko'rsatilgan bo'lsa, o'qishda u JIMGINA
/// <see langword="null"/> bo'lib qaytadi (<c>Tenant.DefaultRawWarehouseId</c> izohi: bu
/// ustunlarda FK yo'q).
/// </para>
/// </remarks>
public class WarehouseDefaultsDto
{
    /// <summary>Tenantning standart xomashyo ombori.</summary>
    public Guid? RawWarehouseId { get; set; }

    /// <summary>Tenantning standart tayyor mahsulot ombori.</summary>
    public Guid? FinishedWarehouseId { get; set; }

    /// <summary>Joriy xodimning shaxsiy standart ombori (tenant sozlamasini bosib ketadi).</summary>
    public Guid? UserWarehouseId { get; set; }

    /// <summary>Kirim formasi ishlatadigan HISOBLANGAN ombor; <see langword="null"/> — forma so'raydi.</summary>
    public Guid? EffectiveRawId { get; set; }

    /// <summary>Chiqim formasi ishlatadigan HISOBLANGAN ombor; <see langword="null"/> — forma so'raydi.</summary>
    public Guid? EffectiveFinishedId { get; set; }
}

/// <summary>Xodimning shaxsiy standart ombori; <see langword="null"/> — shaxsiy tanlov olib tashlanadi.</summary>
public class MyDefaultWarehouseDto
{
    /// <summary>Ombor identifikatori yoki <see langword="null"/>.</summary>
    public Guid? WarehouseId { get; set; }
}
