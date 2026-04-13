using WMS.Application.DTOs.Import;

namespace WMS.Application.Interfaces;

public interface IImportService
{
    Task<ImportResultDto> ImportProductsAsync(int tenantId, Stream stream);
    Task<ImportResultDto> ImportCounterpartiesAsync(int tenantId, Stream stream);
    Task<ImportResultDto> ImportUsersAsync(int tenantId, Stream stream);

    byte[] GenerateProductsTemplate();
    byte[] GenerateCounterpartiesTemplate();
    byte[] GenerateUsersTemplate();
}
