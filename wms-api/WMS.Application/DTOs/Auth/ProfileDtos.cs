namespace WMS.Application.DTOs.Auth;

public class UpdateProfileDto
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
