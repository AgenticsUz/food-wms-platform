using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Common;
using WMS.Application.DTOs.Auth;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _auth;
    private readonly IUserService _users;
    public AuthController(IAuthService auth, IUserService users) { _auth = auth; _users = users; }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _auth.LoginAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _auth.GetCurrentUserAsync(UserId, TenantId);
        return Ok(ApiResponse<UserInfoDto>.Ok(result));
    }

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var permissions = await _users.GetUserPermissionsAsync(TenantId, UserId);
        return Ok(ApiResponse<List<string>>.Ok(permissions));
    }

    [HttpPut("profile")]
    [Authorize(Policy = "MainApi")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        await _auth.UpdateProfileAsync(UserId, TenantId, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Profile updated"));
    }

    [HttpPut("change-password")]
    [Authorize(Policy = "MainApi")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _auth.ChangePasswordAsync(UserId, TenantId, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed"));
    }
}
