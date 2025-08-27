using Api.Exceptions;
using Api.Model;
using Api.Utils;
using Data;
using Microsoft.EntityFrameworkCore;
using Data.Model;
using Api.Extensions;
using Api.Services.Files;

namespace Api.Services;

public interface IDirectoryService
{
    /// <summary>
    /// List the contents of a directory.
    /// </summary>
    /// <param name="path">Path of the directory</param>
    /// <param name="groupId">ID of the group where the directory belongs to</param>
    /// <returns>Name of the requested directory, together with its contents</returns>
    Task<DirectoryDto?> List(int? groupId, string path = "/");

    /// <summary>
    /// Add a new directory at the specified path.
    /// </summary>
    /// <param name="path">Path where the new directory will be added</param>
    /// <param name="groupId">ID of the group where the directory belongs to</param>
    Task AddDirectory(string path, int? groupId);

    /// <summary>
    /// Delete the directory at the specified path.
    /// </summary>
    /// <param name="path">Path to the directory</param>
    /// <param name="groupId">ID of the group where the directory belongs to</param>
    Task DeleteDirectory(string path, int? groupId);
}

public sealed class DirectoryService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IFileStorageService fileStorageService,
    IFileServiceUtils utils,
    IUserService userService,
    IUserGroupService userGroupService) : IDirectoryService
{
    public async Task<DirectoryDto?> List(int? groupId, string path = "/")
    {
        // Ensure that the root directory exists
        if (path is "/")
        {
            var rootDir = await utils.GetDirectory("/", false, groupId);
            if (rootDir is null)
            {
                await AddDirectory("/", groupId);
            }
        }

        var directory = await utils.GetDirectory(path, true, groupId);

        if (directory is null)
        {
            return null;
        }

        return await directory.Decrypt(encryptionService, fileStorageService);
    }

    public async Task AddDirectory(string path, int? groupId)
    {
        var existingDir = await utils.GetDirectory(path, false, groupId);
        if (existingDir is not null)
        {
            throw new DirectoryAlreadyExistsException($"Directory at path '{path}' already exists");
        }

        var user = await userService.GetCurrentUser();

        path = FileUtils.NormalizePath(path);
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        var hashedPath = encryptionService.HashString(path);

        EncryptedDirectory newDirectory;
        if (groupId is null)
        {
            newDirectory = new EncryptedDirectory(encryptedPath, hashedPath, user);
        }
        else
        {
            var group = await userGroupService.GetGroupAsync((int)groupId);
            newDirectory = new EncryptedDirectory(encryptedPath, hashedPath, group);
        }


        if (path is "/")
        {
            context.Directories.Add(newDirectory);
            await context.SaveChangesAsync();
            return;
        }

        var (baseDir, _) = FileUtils.SplitDirAndFile(path);
        var parentDir = await utils.GetDirectory(baseDir, false, groupId);

        // Recursively create parent directories if they don't exist
        if (parentDir is null)
        {
            await AddDirectory(baseDir, groupId);
            parentDir = await utils.GetDirectory(baseDir, false, groupId);
        }

        if (parentDir is null)
        {
            throw new DirectoryNotFoundException($"Parent directory at path '{baseDir}' not found");
        }

        parentDir.Directories.Add(newDirectory);
        await context.SaveChangesAsync();
    }

    public async Task DeleteDirectory(string path, int? groupId)
    {
        var user = await userService.GetCurrentUser();

        path = FileUtils.NormalizePath(path);

        if (path is "/")
        {
            throw new ArgumentException("Cannot delete root directory");
        }

        var directoryQuery = context.Directories
            .Include(dir => dir.Parent)
            .Include(dir => dir.Files)
            .Include(dir => dir.Directories)
            .AsQueryable();

        if (groupId is null)
        {
            directoryQuery = directoryQuery.Where(dir => dir.Owner == user);
        }
        else
        {
            directoryQuery = from dir in directoryQuery
                             join groupRole in context.UserGroupRoles on dir.GroupId equals groupRole.UserGroupId
                             where groupRole.UserId == user.Id && dir.GroupId == groupId
                             select dir;
        }

        var pathHash = encryptionService.HashString(path);
        var directory = await directoryQuery
            .FirstOrDefaultAsync(x => x.PathHash == pathHash);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{path}' not found");
        }

        if (directory.Files.Count != 0 || directory.Directories.Count != 0)
        {
            throw new DirectoryNotEmptyException($"Directory at path '{path}' is not empty");
        }

        var parent = directory.Parent;
        if (parent is null)
        {
            throw new DirectoryNotFoundException($"Parent directory for '{path}' not found");
        }

        parent.Directories.Remove(directory);
        context.Entry(directory).State = EntityState.Deleted;
        await context.SaveChangesAsync();
    }
}
