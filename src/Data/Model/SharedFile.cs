using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("shared_files")]
public sealed class SharedFile
{
    public int Id { get; init; }
    [ForeignKey("EncryptedFileId")] public EncryptedFile? EncryptedFile { get; init; }
    [ForeignKey("EncryptedFileId")] public int EncryptedFileId { get; init; }
    [ForeignKey("SharedToId")] public User? SharedTo { get; init; }
    [ForeignKey("SharedToId")] public int SharedToId { get; init; }
    public DateTime Created { get; init; } = DateTime.UtcNow;
}
