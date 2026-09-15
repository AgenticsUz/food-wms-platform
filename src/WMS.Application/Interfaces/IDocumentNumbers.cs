namespace WMS.Application.Interfaces;

/// <summary>
/// Tenant ichidagi ketma-ket hujjat raqamlari (P2.4).
/// </summary>
/// <remarks>
/// Raqam AYNAN hujjat saqlanadigan tranzaksiyada olinadi: alohida olib, keyin saqlash
/// yiqilsa raqam bekorga yonardi. Bo'shliq baribir bo'lishi mumkin (tranzaksiya qaytsa)
/// — bu qabul qilingan (P2.4 qabul mezoni).
/// </remarks>
public interface IDocumentNumbers
{
    /// <summary>Keyingi raqamni ajratadi.</summary>
    /// <param name="kind">Hisoblagich turi (<c>TenantCounter.Kinds</c>).</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>1 dan boshlanadigan navbatdagi raqam.</returns>
    Task<int> NextAsync(string kind, CancellationToken cancellationToken = default);
}
