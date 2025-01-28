using System.ComponentModel.DataAnnotations;

namespace Server.Db.Model;

public class FileTag
{
    public int Id { get; set; }
    [MaxLength(128)]
    public required string Name { get; set; }
    public List<EncryptedFile> Files { get; set; } = [];
    public User? Owner { get; set; }
    public int OwnerId { get; set; }
}