using System.Text.Json.Serialization;

namespace Api.Model;

public record UserCredentials(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("password")]
    string Password
);