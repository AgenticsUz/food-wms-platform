using System.Text.Json;
using System.Text.Json.Serialization;

namespace WMS.Application.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<UserRoleDto> Roles { get; set; } = new();
}

public class UserRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
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
    [JsonConverter(typeof(FlexibleIntListConverter))]
    public List<int> PermissionIds { get; set; } = new();
}

/// <summary>
/// Accepts both [1,2,3] and ["1","2","3"] from JSON.
/// </summary>
public class FlexibleIntListConverter : JsonConverter<List<int>>
{
    public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<int>();
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType == JsonTokenType.Null)
                continue;
            if (reader.TokenType == JsonTokenType.Number)
                list.Add(reader.GetInt32());
            else if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var val))
                list.Add(val);
            else
                continue; // skip unrecognized values
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var v in value) writer.WriteNumberValue(v);
        writer.WriteEndArray();
    }
}
