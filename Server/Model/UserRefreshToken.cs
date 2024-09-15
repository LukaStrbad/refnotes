using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Server.Model;

public class UserRefreshToken
{
    [Key]
    public int Id { get; set; }
    
    [StringLength(256)]
    [JsonPropertyName("username")]
    public required string Username { get; init; }
    
    public required string RefreshToken { get; set; }
    
    public required DateTime ExpiryTime { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiryTime;
}