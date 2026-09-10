using WMS.Application.DTOs.Import;

namespace WMS.Application.Interfaces;

/// <remarks>
/// F6: <c>tenantId</c> parametrlari o'chdi (D4). Foydalanuvchi importi (va uning shabloni)
/// O'CHDI (D7): foydalanuvchi Identity'da, Console orqali yaratiladi — WMS'da parol yo'q.
/// </remarks>
public interface IImportService
{
    Task<ImportResultDto> ImportProductsAsync(Stream stream);
    Task<ImportResultDto> ImportCounterpartiesAsync(Stream stream);

    byte[] GenerateProductsTemplate();
    byte[] GenerateCounterpartiesTemplate();
}
