using WMS.Application.DTOs.Auth;

namespace WMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<UserInfoDto> GetCurrentUserAsync(int userId, int tenantId);
    Task UpdateProfileAsync(int userId, int tenantId, UpdateProfileDto dto);
    Task ChangePasswordAsync(int userId, int tenantId, ChangePasswordDto dto);
    Task SetTelegramChatAsync(int userId, int tenantId, SetTelegramDto dto);
}
