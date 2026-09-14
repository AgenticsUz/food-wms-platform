using WMS.Domain.Entities;

namespace WMS.Application.Interfaces;

/// <summary>
/// FEFO (muddati birinchi tugaydigan partiya birinchi chiqadi) bo'yicha zaxira yechish —
/// transfer va ishlab chiqarish modullarining UMUMIY yordamchisi (W1·1 moduli egasi).
/// </summary>
/// <remarks>
/// <para>
/// SQLite davrida transfer va ishlab chiqarish servislari FEFO'ning o'z nusxasini yozardi va
/// «mavjud = Quantity - ReservedQuantity &gt; 0» shartini XOTIRADA filtrlardi (SQLite decimal'ni
/// server tomonda solishtira olmasdi). Endi filtr ham, FEFO tartibi ham SQL'da va bitta joyda —
/// ikki nusxa vaqt o'tib bir-biridan ajralmasin.
/// </para>
/// <para>
/// ⚠️ Yordamchi <c>SaveChangesAsync</c> CHAQIRMAYDI: u joriy so'rovning <c>WmsDbContext</c>idagi
/// kuzatilayotgan qatorlarni o'zgartiradi, chaqiruvchi esa hamma balans o'zgarishini BITTA
/// <c>SaveChangesAsync</c> bilan yozadi. Shunda parallel ikki yechishning ikkinchisi <c>xmin</c>
/// tufayli 409 oladi va hech narsa yarim yozilmaydi (D13).
/// </para>
/// </remarks>
public interface IStockAllocator
{
    /// <summary>
    /// <paramref name="quantity"/> ni FEFO bo'yicha yechadi: muddati eng yaqin partiya birinchi,
    /// muddatsiz partiyalar oxirida; zaxiradagi (<c>ReservedQuantity</c>) qismga tegilmaydi.
    /// </summary>
    /// <param name="productId">Mahsulot.</param>
    /// <param name="quantity">Yechiladigan miqdor; <c>&lt;= 0</c> — hech narsa qilinmaydi.</param>
    /// <param name="warehouseId">Manba ombor; <see langword="null"/> — tenantning HAMMA omborlari
    /// (ishlab chiqarish xomashyoni shunday yechadi).</param>
    /// <param name="reduceBatchRemaining"><see langword="true"/> — partiyaning
    /// <c>RemainingQuantity</c> ham kamayadi (tovar kompaniyadan chiqadi: sotuv, ishlab chiqarish);
    /// <see langword="false"/> — ichki ko'chirish, partiya qoldig'i o'zgarmaydi.</param>
    /// <param name="cancellationToken">Bekor qilish.</param>
    /// <returns>
    /// Yetarli bo'lsa — qaysi qatordan qancha olingani (<see cref="FefoAllocation.Lines"/>,
    /// qatorlar kuzatuvda va allaqachon kamaytirilgan). Yetmasa — <see cref="FefoAllocation.Shortfall"/>
    /// &gt; 0 va HECH BIR qator o'zgartirilmagan: chaqiruvchi o'z xabari bilan xato tashlaydi.
    /// </returns>
    Task<FefoAllocation> DeductFefoAsync(
        Guid productId,
        decimal quantity,
        Guid? warehouseId = null,
        bool reduceBatchRemaining = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Berilgan mahsulotlar bo'yicha MAVJUD miqdor (zaxiradagisi ayrilgan holda).
    /// </summary>
    /// <remarks>
    /// ⚠️ Shart va ko'rinish <see cref="DeductFefoAsync"/> BILAN BIR XIL bo'lishi shart:
    /// tekshiruv «yetadi» deb ruxsat bergan hujjat tasdiqda «yetmadi» deb yiqilmasin.
    /// Shuning uchun ikkalasi bitta so'rov shaklidan quriladi.
    /// </remarks>
    /// <param name="productIds">Mahsulotlar; bo'sh bo'lsa bo'sh lug'at qaytadi.</param>
    /// <param name="warehouseId">Ombor; <see langword="null"/> — hamma ombor.</param>
    /// <param name="cancellationToken">Bekor qilish.</param>
    /// <returns>Mahsulot → mavjud miqdor. Qatori yo'q mahsulot lug'atda ham YO'Q.</returns>
    Task<Dictionary<Guid, decimal>> GetAvailableAsync(
        IReadOnlyCollection<Guid> productIds,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>FEFO bo'yicha bitta zaxira qatoridan olingan miqdor.</summary>
/// <param name="Stock">Kuzatilayotgan zaxira qatori (<c>Batch</c> navigatsiyasi yuklangan).</param>
/// <param name="Quantity">Shu qatordan olingan miqdor.</param>
public sealed record FefoLine(WarehouseStock Stock, decimal Quantity);

/// <summary>FEFO yechish natijasi.</summary>
/// <param name="Lines">Qaysi qatordan qancha olingani (FEFO tartibida).</param>
/// <param name="Shortfall">Yetmagan miqdor; <c>0</c> — to'liq yechildi.</param>
public sealed record FefoAllocation(IReadOnlyList<FefoLine> Lines, decimal Shortfall)
{
    public bool IsSatisfied => Shortfall <= 0;
}
