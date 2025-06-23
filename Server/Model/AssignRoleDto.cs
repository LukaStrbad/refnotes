using System.Text.Json.Serialization;
using Data.Db.Model;

namespace Server.Model;

public record AssignRoleDto(
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("role")] UserGroupRoleType Role
);