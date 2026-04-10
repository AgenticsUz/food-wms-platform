using WMS.Application.DTOs.Auth;

namespace WMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserInfoDto> GetCurrentUserAsync(int userId, int tenantId);
}
