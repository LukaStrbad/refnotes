using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Server.Services;

namespace Server.Model;

public class EncryptedFile(string filesystemName, string name)
{
    public int Id { get; set; }
    [MaxLength(255)]
    public string FilesystemName { get; init; } = filesystemName;
    [MaxLength(255)]
    public string Name { get; init; } = name;

    public string DecryptedName(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Name);
    } 
}
