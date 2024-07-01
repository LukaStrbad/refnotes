using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Server.Model;

public class User(
    int id,
    string username,
    string name,
    string email,
    string password,
    string[]? roles
)
{
    [JsonPropertyName("id")] public int Id { get; init; } = id;

    [StringLength(256)]
    [JsonPropertyName("username")]
    public string Username { get; init; } = username;

    [StringLength(256)]
    [JsonPropertyName("name")]
    public string Name { get; init; } = name;

    [StringLength(1024)]
    [JsonPropertyName("email")]
    public string Email { get; init; } = email;

    [StringLength(4096)]
    [JsonPropertyName("password")]
    public string Password { get; set; } = password;

    [JsonPropertyName("roles")] public string[]? Roles { get; set; } = roles;
}