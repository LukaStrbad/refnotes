using Api.Exceptions;
using Api.Extensions;
using Api.Model;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Files;

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
    Task<DateTime> UpdateTimestamp(string directoryPath, string name, int? groupId);
    
    /// <summary>
    /// Update the timestamp of a file in the specified directory.
    /// </summary>
    /// <param name="file">The encrypted file to update</param>
    Task<DateTime> UpdateTimestamp(EncryptedFile file);

    /// <summary>
    /// Get information about a file at the specified path.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    Task<FileResponse> GetFileInfo(string filePath, int? groupId);

    /// <summary>
    /// Get information about a file from its ID.
    /// </summary>
    /// <param name="fileId">File ID</param>
    Task<FileResponse?> GetFileInfoAsync(int fileId);

    /// <summary>
    /// Get the ID of a file at the specified path.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <param name="groupId">ID of the group where the file belongs to</param>
    /// <returns>EncryptedFile object</returns>
    Task<EncryptedFile?> GetEncryptedFileAsync(string filePath, int? groupId);

    Task<User?> GetUserFromFile(EncryptedFile encryptedFile);

    Task<UserGroup?> GetUserGroupFromFile(EncryptedFile encryptedFile);

    /// <summary>
    /// Gets the file owner of the group
    /// </summary>
    /// <param name="encryptedFile">the encrypted file</param>
    Task<DirOwner> GetDirOwnerAsync(EncryptedFile encryptedFile);

    /// <summary>
    /// Gets the encrypted file from the specified path for the specified user.
    /// </summary>
    /// <remarks>This method doesn't validate file ownership</remarks>
    /// <param name="filePath">Path of the file</param>
    /// <param name="user">Owner of the file</param>
    Task<EncryptedFile?> GetEncryptedFileForUserAsync(string filePath, User user);

    /// <summary>
    /// Gets the encrypted file from the specified path for the specified group.
    /// </summary>
    /// <remarks>This method doesn't validate if the user has access to the group</remarks>
    /// <param name="filePath">Path of the file</param>
    /// <param name="group">Group of the file</param>
    /// <returns></returns>
    Task<EncryptedFile?> GetEncryptedFileForGroupAsync(string filePath, UserGroup group);

    Task<EncryptedFile?> GetEncryptedFileForOwnerAsync(string filePath, DirOwner owner);

    /// <summary>
    /// Gets the full path of a file from its ID.
    /// </summary>
    /// <param name="file">The file</param>
    /// <returns>A string with the full path of the requested file</returns>
    Task<string> GetFilePathAsync(EncryptedFile file);

    Task<EncryptedFile?> GetEncryptedFileByRelativePathAsync(EncryptedFile encryptedFile, string relativePath);

    Task<GroupDetails?> GetGroupDetailsFromFileIdAsync(int encryptedFileId);
}

public class FileService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IFileStorageService fileStorageService,
    IFileServiceUtils utils,
    IUserGroupService userGroupService) : IFileService
{
    public async Task<string> AddFile(string directoryPath, string name, int? groupId)
    {
        var directory = await utils.GetDirectory(directoryPath, true, groupId);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{directoryPath}' not found");
        }

        var nameHash = encryptionService.HashString(name);
        if (directory.Files.Any(x => x.NameHash == nameHash))
        {
            throw new FileAlreadyExistsException(
                $"File with name '{name}' already exists in directory '{directoryPath}'");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var encryptedFile = new EncryptedFile(GenerateFilesystemName(), encryptedName, nameHash);
        directory.Files.Add(encryptedFile);
        await context.SaveChangesAsync();
        return encryptedFile.FilesystemName;
    }

    public async Task MoveFile(string oldName, string newName, int? groupId)
    {
        var (dirName, filename) = FileUtils.SplitDirAndFile(oldName);
        var (newDirName, newFilename) = FileUtils.SplitDirAndFile(newName);

        var (dir, file) = await utils.GetDirAndFile(dirName, filename, groupId);
        // If directory is the same, use the existing directory
        var newDir = newDirName == dirName
            ? dir
            : await utils.GetDirectory(newDirName, true, groupId);

        if (newDir is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{newDirName}' not found");
        }

        var nameHash = encryptionService.HashString(newFilename);
        if (newDir.Files.Any(f => f.NameHash == nameHash))
        {
            throw new FileAlreadyExistsException(
                $"File with name {newFilename} already exists in directory {newDirName}");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(newFilename);
        dir.Files.Remove(file);
        newDir.Files.Add(file);
        file.Name = encryptedName;
        file.NameHash = nameHash;
        file.Modified = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private static string GenerateFilesystemName()
    {
        // Generate a new version 7 GUID for the file name
        // This has a very low probability of collision
        var name = Guid.CreateVersion7();
        // Append bin to the end as this file is encrypted and cannot be read as text or anything else
        return $"{name}.bin";
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

    public async Task<DateTime> UpdateTimestamp(string directoryPath, string name, int? groupId)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId);
        await UpdateTimestamp(file);
        return file.Modified;
    }

    public async Task<DateTime> UpdateTimestamp(EncryptedFile file)
    {
        var modified = DateTime.UtcNow;
        file.Modified = modified;
        context.Entry(file).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return modified;
    }

    public async Task<FileResponse> GetFileInfo(string filePath, int? groupId)
    {
        var (directoryPath, name) = FileUtils.SplitDirAndFile(filePath);
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId, includeTags: true);
        var fileSize = await fileStorageService.GetFileSize(file.FilesystemName);

        return new FileResponse(name,
            filePath,
            file.Tags.Select(tag => tag.DecryptedName(encryptionService)),
            fileSize,
            file.Created,
            file.Modified);
    }

    public async Task<FileResponse?> GetFileInfoAsync(int fileId)
    {
        var file = await context.Files.FindAsync(fileId);

        if (file is null)
            return null;

        var encryptedDirectoryPath = await context.Entry(file).Reference(f => f.EncryptedDirectory)
            .Query().Select(d => d.Path).FirstAsync();

        var directoryPath = encryptionService.DecryptAesStringBase64(encryptedDirectoryPath);
        var fileSize = await fileStorageService.GetFileSize(file.FilesystemName);

        var fileName = file.DecryptedName(encryptionService);
        var fullPath = FileUtils.NormalizePath(Path.Join(directoryPath, fileName));

        var fileTags = await context.Entry(file).Collection(f => f.Tags).Query()
            .Select(t => t.Name)
            .ToListAsync();

        return new FileResponse(fileName,
            fullPath,
            fileTags.Select(encryptionService.DecryptAesStringBase64),
            fileSize,
            file.Created,
            file.Modified);
    }

    public async Task<EncryptedFile?> GetEncryptedFileAsync(string filePath, int? groupId)
    {
        var (directoryPath, name) = FileUtils.SplitDirAndFile(filePath);
        try
        {
            var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId);

            return file;
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public async Task<User?> GetUserFromFile(EncryptedFile encryptedFile)
    {
        await context.Entry(encryptedFile).Reference(f => f.EncryptedDirectory).LoadAsync();
        if (encryptedFile.EncryptedDirectory is null)
            throw new Exception("File has no directory");

        var directory = encryptedFile.EncryptedDirectory;
        await context.Entry(directory).Reference(d => d.Owner).LoadAsync();
        return directory.Owner;
    }

    public async Task<UserGroup?> GetUserGroupFromFile(EncryptedFile encryptedFile)
    {
        await context.Entry(encryptedFile).Reference(f => f.EncryptedDirectory).LoadAsync();
        if (encryptedFile.EncryptedDirectory is null)
            throw new Exception("File has no directory");

        var directory = encryptedFile.EncryptedDirectory;
        await context.Entry(directory).Reference(d => d.Group).LoadAsync();
        return directory.Group;
    }

    public async Task<DirOwner> GetDirOwnerAsync(EncryptedFile encryptedFile)
    {
        await context.Entry(encryptedFile).Reference(f => f.EncryptedDirectory).LoadAsync();
        if (encryptedFile.EncryptedDirectory is null)
            throw new Exception("File has no directory");

        var directory = encryptedFile.EncryptedDirectory;
        await context.Entry(directory).Reference(d => d.Owner).LoadAsync();
        if (directory.Owner is not null)
            return new DirOwner(directory.Owner);

        await context.Entry(directory).Reference(d => d.Group).LoadAsync();
        if (directory.Group is not null)
            return new DirOwner(directory.Group);

        throw new Exception("File has no owner or group");
    }

    public async Task<EncryptedFile?> GetEncryptedFileForUserAsync(string filePath, User user)
    {
        var (directoryPath, name) = FileUtils.SplitDirAndFile(filePath);
        var directoryPathHash = encryptionService.HashString(directoryPath);
        var nameHash = encryptionService.HashString(name);

        var dir = await context.Directories
            .Where(x => x.Owner == user)
            .FirstOrDefaultAsync(x => x.PathHash == directoryPathHash);
        if (dir is null)
            return null;

        var file = await context.Files
            .Where(x => x.EncryptedDirectory == dir)
            .FirstOrDefaultAsync(x => x.NameHash == nameHash);

        return file;
    }

    public async Task<EncryptedFile?> GetEncryptedFileForGroupAsync(string filePath, UserGroup group)
    {
        var (directoryPath, name) = FileUtils.SplitDirAndFile(filePath);
        var directoryPathHash = encryptionService.HashString(directoryPath);
        var nameHash = encryptionService.HashString(name);

        var dir = await context.Directories
            .Where(x => x.Group == group)
            .FirstOrDefaultAsync(x => x.PathHash == directoryPathHash);
        if (dir is null)
            return null;

        var file = await context.Files
            .Where(x => x.EncryptedDirectory == dir)
            .FirstOrDefaultAsync(x => x.NameHash == nameHash);

        return file;
    }

    public async Task<EncryptedFile?> GetEncryptedFileForOwnerAsync(string filePath, DirOwner owner)
    {
        if (owner.User is not null)
            return await GetEncryptedFileForUserAsync(filePath, owner.User);

        if (owner.Group is not null)
            return await GetEncryptedFileForGroupAsync(filePath, owner.Group);

        throw new Exception("File owner is not a user or group");
    }

    public async Task<string> GetFilePathAsync(EncryptedFile file)
    {
        await context.Entry(file).Reference(f => f.EncryptedDirectory).LoadAsync();

        if (file.EncryptedDirectory is null)
            throw new DirectoryNotFoundException("File directory not found");

        var dirPath = file.EncryptedDirectory.DecryptedPath(encryptionService);
        var fileName = file.DecryptedName(encryptionService);
        return FileUtils.NormalizePath(Path.Join(dirPath, fileName));
    }

    public async Task<EncryptedFile?> GetEncryptedFileByRelativePathAsync(EncryptedFile encryptedFile,
        string relativePath)
    {
        await context.Entry(encryptedFile).Reference(f => f.EncryptedDirectory).LoadAsync();
        if (encryptedFile.EncryptedDirectory is null)
            throw new Exception("File has no directory");

        var directory = encryptedFile.EncryptedDirectory;
        await context.Entry(directory).Reference(d => d.Owner).LoadAsync();
        await context.Entry(directory).Reference(d => d.Group).LoadAsync();

        try
        {
            // Get the directory of the file
            var dirPath = directory.DecryptedPath(encryptionService);
            // Resolve the relative path of the file
            var absoluteFilePath = FileUtils.ResolveRelativeFolderPath(dirPath, relativePath);

            if (directory.Group is not null)
                return await GetEncryptedFileForGroupAsync(absoluteFilePath, directory.Group);

            if (directory.Owner is not null)
                return await GetEncryptedFileForUserAsync(absoluteFilePath, directory.Owner);

            throw new Exception("File has no owner or group");
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public async Task<GroupDetails?> GetGroupDetailsFromFileIdAsync(int encryptedFileId)
    {
        var query = from file in context.Files
            join directory in context.Directories on file.EncryptedDirectoryId equals directory.Id
            where file.Id == encryptedFileId
            select directory.GroupId;

        var groupId = await query.FirstOrDefaultAsync();
        if (groupId is null)
            return null;

        return await userGroupService.GetGroupDetailsAsync(groupId.Value);
    }
}
