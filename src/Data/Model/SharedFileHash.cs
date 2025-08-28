using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Model;

[Table("shared_file_hashes")]
[Index(nameof(Hash))]
public sealed class SharedFileHash
{
    public int Id { get; init; }
    public required EncryptedFile EncryptedFile { get; init; }
    [MaxLength(256)]
    public required string Hash { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
