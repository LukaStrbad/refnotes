using Server.Db.Model;
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
            file.Tags.Select(tag => tag.DecryptedName(encryptionService)).ToList());
    }
}