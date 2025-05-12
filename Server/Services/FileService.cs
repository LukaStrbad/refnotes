using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Utils;

namespace Server.Services;

public interface IFileService
{
    /// <summary>
    /// Add a new file to the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory where the file will be added</param>
    /// <param name="name">Name of the file to be added</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    /// <returns>Filesystem name of the added file</returns>
    Task<string> AddFile(string directoryPath, string name, int? groupId);

    /// <summary>
    /// Moves the existing file
    /// </summary>
    /// <param name="oldName">Full path of the old file</param>
    /// <param name="newName">Full path of the new file</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    /// <returns></returns>
    Task MoveFile(string oldName, string newName, int? groupId);

    /// <summary>
    /// Get the filesystem path of a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    /// <returns>Filesystem path of the file, or null if not found</returns>
    Task<string?> GetFilesystemFilePath(string directoryPath, string name, int? groupId);

    /// <summary>
    /// Delete a file from the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to be deleted</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    Task DeleteFile(string directoryPath, string name, int? groupId);

    /// <summary>
    /// Update the timestamp of a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to update</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    Task UpdateTimestamp(string directoryPath, string name, int? groupId);

    /// <summary>
    /// Get information about a file at the specified path.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    /// <returns></returns>
    Task<FileDto> GetFileInfo(string filePath, int? groupId);
}

public class FileService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IFileStorageService fileStorageService,
    AppConfiguration appConfiguration,
    IFileServiceUtils utils) : IFileService
{
    public async Task<string> AddFile(string directoryPath, string name, int? groupId)
    {
        var directory = await utils.GetDirectory(directoryPath, true, groupId);

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

    public async Task MoveFile(string oldName, string newName, int? groupId)
    {
        var (dirName, filename) = FileServiceUtils.SplitDirAndFile(oldName);
        var (newDirName, newFilename) = FileServiceUtils.SplitDirAndFile(newName);

        var (dir, file) = await utils.GetDirAndFile(dirName, filename, groupId);
        // If directory is the same, use the existing directory
        var newDir = newDirName == dirName
            ? dir
            : await utils.GetDirectory(newDirName, true, groupId);

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

    public async Task<string?> GetFilesystemFilePath(string directoryPath, string name, int? groupId)
    {
        try
        {
            var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId);
            return file.FilesystemName;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public async Task DeleteFile(string directoryPath, string name, int? groupId)
    {
        var (directory, file) = await utils.GetDirAndFile(directoryPath, name, groupId);

        directory.Files.Remove(file);
        context.Entry(file).State = EntityState.Deleted;
        await context.SaveChangesAsync();
    }

    public async Task UpdateTimestamp(string directoryPath, string name, int? groupId)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId);
        file.Modified = DateTime.UtcNow;
        context.Entry(file).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    public async Task<FileDto> GetFileInfo(string filePath, int? groupId)
    {
        var (directoryPath, name) = FileServiceUtils.SplitDirAndFile(filePath);
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId, includeTags: true);
        var fileSize = await fileStorageService.GetFileSize(file.FilesystemName);

        return new FileDto(file.DecryptedName(encryptionService),
            file.Tags.Select(tag => tag.DecryptedName(encryptionService)),
            fileSize,
            file.Created,
            file.Modified);
    }
}