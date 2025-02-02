using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Server.Services;

namespace Server.Db.Model;

public class FileTag(string name, int ownerId)
{
    public int Id { get; set; }
    [MaxLength(128)] public string Name { get; set; } = name;
    public List<EncryptedFile> Files { get; set; } = [];
    public User? Owner { get; set; }
    public int OwnerId { get; set; } = ownerId;
    
    public string DecryptedName(IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(Name);
    }
}