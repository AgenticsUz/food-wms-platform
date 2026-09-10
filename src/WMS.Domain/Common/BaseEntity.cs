namespace WMS.Domain.Common;

/// <summary>
/// Barcha jadvallarning asosi.
/// </summary>
/// <remarks>
/// ⚠️ Kalit <c>Guid</c> v7 va u MIJOZ tomonda beriladi (F6, PLATFORMA-TZ §7·F6 D3).
/// SQLite davrida <c>int</c> identity edi; endi tenant ham, foydalanuvchi ham
/// Identity'dan Guid bo'lib keladi va ularga ishora qiluvchi hamma kalit bir xil
/// turda bo'lsin. EF tomonida <c>ValueGenerated.Never</c> — sababi
/// <c>WmsDbContext.ApplyClientGeneratedKeyConvention</c> izohida (F4 dagi
/// «INSERT o'rniga UPDATE» tuzog'i).
/// </remarks>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Tenantga tegishli jadval — Postgres RLS (<c>app.tenant_id</c>) ostida turadi.
/// </summary>
/// <remarks>
/// <c>TenantId</c> ni servis QO'YMAYDI: <c>WmsDbContext.StampEntries</c> yangi
/// yozuvga joriy tenantni o'zi yozadi va begona tenantga yozishni rad etadi.
/// Bu SQLite davridagi ~245 ta qo'lda yozilgan <c>TenantId == tenantId</c>
/// shartining o'rnini bosadi (D4).
/// </remarks>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary><see cref="ITenantEntity"/> ning standart asosi.</summary>
public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}
