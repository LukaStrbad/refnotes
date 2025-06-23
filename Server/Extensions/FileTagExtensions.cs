using Data.Model;
using Server.Services;

namespace Server.Extensions;

public static class FileTagExtensions
{
    public static string DecryptedName(this FileTag fileTag, IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(fileTag.Name);
    }
}