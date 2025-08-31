using Api.Services;
using Data.Model;

namespace Api.Extensions;

public static class EncryptedDirectoryExtensions
{
    public static string DecryptedPath(this EncryptedDirectory directory, IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(directory.Path);
    }

    public static string DecryptedName(this EncryptedDirectory directory, IEncryptionService encryptionService)
    {
        var path = directory.DecryptedPath(encryptionService);
        return path == "/" ? "/" : Path.GetFileName(path);
    }
}
