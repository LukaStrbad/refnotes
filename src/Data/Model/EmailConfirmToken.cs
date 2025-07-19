using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Model;

[Table("email_confirm_tokens")]
[Index(nameof(Value))]
public sealed class EmailConfirmToken
{
    public int Id { get; init; }

    [StringLength(128)] public required string Value { get; set; }

    public User? User { get; set; }
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
