using Api.Model;
using Api.Services;
using Api.Utils;
using Data.Model;

namespace Api.Extensions;

public static class EncryptedDirectoryExtensions
{
    public static async Task<DirectoryDto> Decrypt(
        this EncryptedDirectory encryptedDirectory,
        IEncryptionService encryptionService,
        IFileStorageService fileStorageService)
    {
        var name = encryptedDirectory.DecryptedName(encryptionService);
        var dirPath = encryptedDirectory.DecryptedPath(encryptionService);
        var filesTasks = encryptedDirectory.Files.Select(async file =>
        {
            var fileName = file.DecryptedName(encryptionService);
            var filePath = FileUtils.NormalizePath($"{dirPath}/{fileName}");
            var tags = file.Tags.Select(tag => tag.DecryptedName(encryptionService));
            var size = await fileStorageService.GetFileSize(file.FilesystemName);
            return new FileDto(fileName, filePath, tags, size, file.Created, file.Modified);
        });
        var files = await Task.WhenAll(filesTasks);

        var directoriesTasks = encryptedDirectory.Directories.Select(async directory =>
            (await directory.Decrypt(encryptionService, fileStorageService)).Name);
        var directories = await Task.WhenAll(directoriesTasks);

        return new DirectoryDto(name, files, directories);
    }

    public static string DecryptedPath(this EncryptedDirectory directory, IEncryptionService encryptionService)
    {
        return encryptionService.DecryptAesStringBase64(directory.Path);
    }

    public static string DecryptedName(this EncryptedDirectory directory, IEncryptionService encryptionService)
    {
        var path = directory.DecryptedPath(encryptionService);
        return path == "/" ? "/" : System.IO.Path.GetFileName(path);
    }
}