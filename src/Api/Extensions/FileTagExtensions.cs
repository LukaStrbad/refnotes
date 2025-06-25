using Api.Services;
using Data.Model;

namespace Api.Extensions;

public static class FileTagExtensions
{
    public static string DecryptedName(this FileTag fileTag, IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(fileTag.Name);
    }
}