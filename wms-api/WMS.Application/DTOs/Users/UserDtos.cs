namespace WMS.Application.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserDto
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class UpdateUserDto
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Password { get; set; }
}

public class AssignRolesDto
{
    public List<int> RoleIds { get; set; } = new();
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class CreateRoleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateRoleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string? Description { get; set; }
}

public class AssignPermissionsDto
{
    public List<int> PermissionIds { get; set; } = new();
}
