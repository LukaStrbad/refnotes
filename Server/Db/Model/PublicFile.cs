using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Server.Db.Model;

[Table("public_files")]
[Index(nameof(UrlHash), IsUnique = true)]
public class PublicFile
{
    public int Id { get; init; }
    
    [MaxLength(256)]
    public required string UrlHash { get; init; }

    [ForeignKey("EncryptedFileId")] public EncryptedFile? EncryptedFile { get; init; }

    [ForeignKey("EncryptedFileId")] public int EncryptedFileId { get; init; }
    
    public DateTime Created { get; init; } = DateTime.UtcNow;
}