using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Server.Services;

namespace Server.Model;

public class EncryptedFile
{
    public int Id { get; set; }
    [MaxLength(255)]
    public required string FilesystemName { get; init; }
    [MaxLength(255)]
    public required string Name { get; init; }

    public string DecryptedName(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Name);
    } 
}
