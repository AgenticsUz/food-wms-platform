namespace WMS.Application.DTOs.Analytics;

/// <summary>
/// Mahsulot bo'yicha foyda (P2.5): sotuv tushumi minus SOTILGAN partiyalarning tannarxi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <see cref="UnknownCostQuantity"/> — hisobotning eng muhim maydoni. Tannarxi noma'lum
/// qator COST ga 0 deb qo'shilmaydi: qo'shilsa, o'sha miqdor sof foyda bo'lib ko'rinib,
/// hisobot foydani SOXTA katta ko'rsatardi. Shuning uchun bunday qatorlar tushumda
/// (<see cref="Revenue"/>) va miqdorda (<see cref="Quantity"/>) qoladi, lekin tannarxdan
/// TASHQARIDA turadi va alohida raqam bilan e'lon qilinadi. <see cref="IsCostComplete"/>
/// <see langword="false"/> bo'lsa, <see cref="Profit"/> — YUQORI CHEGARA, aniq raqam emas;
/// UI uni shunday ko'rsatishi kerak.
/// </para>
/// <para>
/// Tannarx qayerdan: <c>TransferItem.UnitCost</c> — chiqim lahzasida FEFO tanlagan
/// partiyaning tannarxi (NUSXA). Partiya keyin o'chirilsa ham foyda o'zgarmaydi.
/// </para>
/// </remarks>
public sealed class ProductProfitDto
{
    /// <summary>Mahsulot.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Mahsulot nomi.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Davrda sotilgan umumiy miqdor (tannarxi noma'lum qatorlar ham ichida).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Tushum — <c>Σ(miqdor × sotuv narxi)</c>.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Tannarx — <c>Σ(miqdor × partiya tannarxi)</c>, FAQAT tannarxi ma'lum qatorlar.</summary>
    public decimal Cost { get; set; }

    /// <summary>Tannarxi noma'lum (<c>UnitCost = null</c>) qatorlardagi miqdor.</summary>
    public decimal UnknownCostQuantity { get; set; }

    /// <summary>Foyda. <see cref="IsCostComplete"/> <see langword="false"/> bo'lsa — yuqori chegara.</summary>
    public decimal Profit => Revenue - Cost;

    /// <summary>Foyda ulushi (%), tushumga nisbatan.</summary>
    public decimal Margin => Revenue > 0 ? Math.Round(Profit / Revenue * 100, 2) : 0m;

    /// <summary><see langword="true"/> — davrdagi HAR bir qatorning tannarxi ma'lum.</summary>
    public bool IsCostComplete => UnknownCostQuantity <= 0m;
}
