using Data.Db.Model;
using Server.Model;
using Server.Services;

namespace Server.Extensions;

public static class EncryptedFileExtensions
{
    public static FileSearchResultDto ToSearchResultDto(this EncryptedFile file, string directoryPath,
        IEncryptionService encryptionService)
    {
        return new FileSearchResultDto(
            $"{directoryPath}/{file.DecryptedName(encryptionService)}".Replace("//", "/"),
            file.Tags.Select(tag => tag.DecryptedName(encryptionService)).ToList(),
            file.FilesystemName,
            file.Modified);
    }
    
    public static string DecryptedName(this EncryptedFile file, IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(file.Name);
    }
}