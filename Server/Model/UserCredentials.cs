using System.Text.Json.Serialization;

namespace Server.Model;

public record UserCredentials(
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("password")]
    string Password
);