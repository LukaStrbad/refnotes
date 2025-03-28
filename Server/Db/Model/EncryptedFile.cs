using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Server.Services;

namespace Server.Db.Model;

[Table("encrypted_files")]
public class EncryptedFile(string filesystemName, string name)
{
    public int Id { get; set; }
    [MaxLength(255)]
    public string FilesystemName { get; init; } = filesystemName;
    [MaxLength(255)]
    public string Name { get; set; } = name;
    public List<FileTag> Tags { get; init; } = [];

    public string DecryptedName(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Name);
    } 
}
