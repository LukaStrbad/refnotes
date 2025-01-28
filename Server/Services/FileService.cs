using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Utils;

namespace Server.Services;

public interface IFileService
{
    /// <summary>
    /// Add a new file to the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory where the file will be added</param>
    /// <param name="name">Name of the file to be added</param>
    /// <returns>Filesystem name of the added file</returns>
    Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);

    /// <summary>
    /// Get the filesystem path of a file in the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <returns>Filesystem path of the file, or null if not found</returns>
    Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);

    /// <summary>
    /// Delete a file from the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to be deleted</param>
    Task DeleteFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
}

public class FileService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    AppConfiguration appConfiguration) : BaseService(context), IFileService
{
     public async Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var directory = await ServiceUtils.GetDirectory(user, encryptionService, Context, directoryPath, true);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{directoryPath}' not found");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        if (directory.Files.Any(x => x.Name == encryptedName))
        {
            throw new FileAlreadyExistsException(
                $"File with name '{name}' already exists in directory '{directoryPath}'");
        }

        var encryptedFile = new EncryptedFile(GenerateFilesystemName(), encryptedName);
        directory.Files.Add(encryptedFile);
        await Context.SaveChangesAsync();
        return encryptedFile.FilesystemName;
    }
     
    private string GenerateFilesystemName()
    {
        for (var i = 0; i < 100; i++)
        {
            var randomName = Path.GetRandomFileName();
            var baseDir = appConfiguration.DataDir;
            var path = Path.Join(baseDir, randomName);
            if (File.Exists(path)) continue;
            return randomName;
        }

        throw new Exception("Failed to generate unique filesystem name.");
    }

    public async Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = Context.Directories
            .Include(dir => dir.Files)
            .FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);
        return file?.FilesystemName;
    }

    public async Task DeleteFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = Context.Directories
            .Include(dir => dir.Files)
            .FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);

        if (file is null)
        {
            throw new FileNotFoundException($"File with name ${name} not found in directory ${directoryPath}.");
        }

        directory.Files.Remove(file);
        Context.Entry(file).State = EntityState.Deleted;
        await Context.SaveChangesAsync();
    }
}