using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Db.Model;

[Table("encrypted_files")]
[Index(nameof(EncryptedDirectoryId))]
public class EncryptedFile(string filesystemName, string name)
{
    public int Id { get; set; }
    [MaxLength(255)]
    public string FilesystemName { get; init; } = filesystemName;
    [MaxLength(255)]
    public string Name { get; set; } = name;
    public List<FileTag> Tags { get; init; } = [];
    
    public DateTime Created { get; init; } = DateTime.UtcNow;

    public DateTime Modified { get; set; } = DateTime.UtcNow;
    
    [ForeignKey("EncryptedDirectoryId")]
    public EncryptedDirectory? EncryptedDirectory { get; init; }
    
    [ForeignKey("EncryptedDirectoryId")]
    public int EncryptedDirectoryId { get; init; }
}
