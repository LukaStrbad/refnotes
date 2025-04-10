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
    /// <param name="directoryPath">Path of the directory where the file will be added</param>
    /// <param name="name">Name of the file to be added</param>
    /// <returns>Filesystem name of the added file</returns>
    Task<string> AddFile(string directoryPath, string name);

    /// <summary>
    /// Moves the existing file
    /// </summary>
    /// <param name="oldName">Full path of the old file</param>
    /// <param name="newName">Full path of the new file</param>
    /// <returns></returns>
    Task MoveFile(string oldName, string newName);

    /// <summary>
    /// Get the filesystem path of a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <returns>Filesystem path of the file, or null if not found</returns>
    Task<string?> GetFilesystemFilePath(string directoryPath, string name);

    /// <summary>
    /// Delete a file from the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to be deleted</param>
    Task DeleteFile(string directoryPath, string name);

    /// <summary>
    /// Update the timestamp of a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to update</param>
    Task UpdateTimestamp(string directoryPath, string name);
}

public class FileService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    AppConfiguration appConfiguration,
    ServiceUtils utils) : IFileService
{
    public async Task<string> AddFile(string directoryPath, string name)
    {
        var directory = await utils.GetDirectory(directoryPath, true);

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
        await context.SaveChangesAsync();
        return encryptedFile.FilesystemName;
    }

    public async Task MoveFile(string oldName, string newName)
    {
        var (dirName, filename) = ServiceUtils.SplitDirAndFile(oldName);
        var (newDirName, newFilename) = ServiceUtils.SplitDirAndFile(newName);

        var (dir, file) = await utils.GetDirAndFile(dirName, filename);
        // If directory is the same, use the existing directory
        var newDir = newDirName == dirName
            ? dir
            : await utils.GetDirectory(newDirName, true);

        if (newDir is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{newDirName}' not found");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(newFilename);
        if (newDir.Files.Any(f => f.Name == encryptedName))
        {
            throw new FileAlreadyExistsException(
                $"File with name {newFilename} already exists in directory {newDirName}");
        }

        dir.Files.Remove(file);
        newDir.Files.Add(file);
        file.Name = encryptedName;
        file.Modified = DateTime.UtcNow;
        await context.SaveChangesAsync();
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

    public async Task<string?> GetFilesystemFilePath(string directoryPath, string name)
    {
        try
        {
            var (_, file) = await utils.GetDirAndFile(directoryPath, name);
            return file.FilesystemName;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private async Task<(EncryptedDirectory, EncryptedFile, User)> GetDirAndFile(string directoryPath, string name,
        bool includeTags = false)
    {
        var user = await utils.GetUser();
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var query = context.Directories
            .Include(dir => dir.Files)
            .AsQueryable();
        if (includeTags)
        {
            query = query.Include(dir => dir.Files)
                .ThenInclude(file => file.Tags);
        }

        var directory = query.FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

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

        return (directory, file, user);
    }

    public async Task DeleteFile(string directoryPath, string name)
    {
        var (directory, file, _) = await GetDirAndFile(directoryPath, name);

        directory.Files.Remove(file);
        context.Entry(file).State = EntityState.Deleted;
        await context.SaveChangesAsync();
    }

    public async Task UpdateTimestamp(string directoryPath, string name)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name);
        file.Modified = DateTime.UtcNow;
        context.Entry(file).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }
}
