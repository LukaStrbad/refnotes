using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("shared_files")]
public sealed class SharedFile
{
    public int Id { get; init; }
    
    [ForeignKey("SharedEncryptedFileId")] public EncryptedFile? SharedEncryptedFile { get; init; }
    [ForeignKey("SharedEncryptedFileId")] public int SharedEncryptedFileId { get; init; }
    
    [ForeignKey("SharedToDirectoryId")] public EncryptedDirectory? SharedToDirectory { get; init; }
    [ForeignKey("SharedToDirectoryId")] public int SharedToDirectoryId { get; init; }
    
    public DateTime Created { get; init; } = DateTime.UtcNow;
}
