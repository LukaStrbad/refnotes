using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Model;

[Table("public_files")]
[Index(nameof(UrlHash), nameof(State), IsUnique = true)]
public class PublicFile
{
    public int Id { get; init; }

    [MaxLength(256)] public string UrlHash { get; init; }

    [ForeignKey("EncryptedFileId")] public EncryptedFile? EncryptedFile { get; init; }

    [ForeignKey("EncryptedFileId")] public int EncryptedFileId { get; init; }

    public PublicFileState State { get; set; } = PublicFileState.Active;

    public DateTime Created { get; init; } = DateTime.UtcNow;

    public PublicFile(string urlHash, int encryptedFileId)
    {
        UrlHash = urlHash;
        EncryptedFileId = encryptedFileId;
    }
}

public enum PublicFileState
{
    Active,
    Inactive
}
