namespace WMS.Application.DTOs.Pricing;

/// <summary>
/// «Oxirgi narx» taklifi (P2.2) — narxning O'ZI va u QAYERDAN olingani.
/// </summary>
/// <remarks>
/// <para>
/// Manba ataylab qaytariladi: forma «oxirgi: 12 000» deb yozsa, foydalanuvchi «qachon,
/// kimga?» deb so'raydi. Javob bir chaqiriqda bo'lmasa, UI ikkinchi so'rov yuborardi
/// yoki taklif ishonchsiz qolardi.
/// </para>
/// <para>
/// ⚠️ <see cref="IsSameCounterparty"/> — taklifning ISHONCH darajasi: shu mijoz bilan
/// bo'lgan narx uning uchun kelishilgan narx, boshqa mijozniki esa faqat orientir
/// (chegirma shartlari har mijozda har xil). UI ikkalasini bir xil ko'rsatmasligi kerak.
/// </para>
/// </remarks>
public sealed class LastPriceDto
{
    /// <summary>Topilgan narx (hujjat qatoridagi <c>UnitPrice</c>).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Manba hujjatning sanasi (<c>CreatedAt</c> emas — P2.3).</summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>Manba hujjat identifikatori (havola uchun).</summary>
    public Guid TransferId { get; set; }

    /// <summary>Manba hujjatning qisqa raqami (P2.4) — odamga ko'rsatiladigan nom.</summary>
    public int Number { get; set; }

    /// <summary>Manba hujjatdagi kontragent; ichki hujjatda <see langword="null"/>.</summary>
    public Guid? CounterpartyId { get; set; }

    /// <summary>Kontragent nomi (kontragentsiz yoki o'chirilgan bo'lsa <see langword="null"/>).</summary>
    public string? CounterpartyName { get; set; }

    /// <summary>
    /// <see langword="true"/> — narx SO'RALGAN kontragent bilan bo'lgan hujjatdan;
    /// <see langword="false"/> — umumiy (oxirgi narx, kimdan bo'lishidan qat'i nazar).
    /// </summary>
    public bool IsSameCounterparty { get; set; }
}
