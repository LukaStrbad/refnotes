using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Db.Model;

[Table("group_access_codes")]
[Index(nameof(ExpiryTime))]
[Index(nameof(GroupId), nameof(Value))]
public class GroupAccessCode
{
    public int Id { get; set; }

    [StringLength(128)] public required string Value { get; init; }
    public required DateTime ExpiryTime { get; set; }
    public required UserGroup Group { get; init; }
    public int GroupId { get; set; }
    public required User Sender { get; init; }
    public int SenderId { get; init; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiryTime;
}