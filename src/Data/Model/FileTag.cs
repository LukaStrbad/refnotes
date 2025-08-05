using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("file_tags")]
public class FileTag
{
    public int Id { get; set; }
    [MaxLength(128)] public required string Name { get; set; }
    [MaxLength(64)] public required string NameHash { get; set; }
    public List<EncryptedFile> Files { get; set; } = [];
    public User? Owner { get; set; }
    public int? OwnerId { get; set; }

    public UserGroup? GroupOwner { get; set; }
    public int? GroupOwnerId { get; set; }
}
