using Api.Services;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Utils;

public interface IFileServiceUtils
{
    /// <summary>
    /// Gets the directory from the provided path
    /// </summary>
    /// <param name="path">Path to the directory</param>
    /// <param name="includeFilesAndDirs">Whether the directory should be filled with files</param>
    /// <param name="groupId">Group where the directory belongs to</param>
    Task<EncryptedDirectory?> GetDirectory(string path, bool includeFilesAndDirs, int? groupId);

    /// <summary>
    /// Gets directory and file from the given path
    /// </summary>
    /// <param name="directoryPath">Directory path</param>
    /// <param name="name">Filename</param>
    /// <param name="groupId">Group where the directory belongs to</param>
    /// <param name="includeTags">Whether to include file tags</param>
    /// <exception cref="DirectoryNotFoundException">Thrown when directory doesn't exist</exception>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    Task<(EncryptedDirectory, EncryptedFile)> GetDirAndFile(string directoryPath, string name, int? groupId,
        bool includeTags = false);
}

public class FileServiceUtils(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IUserService userService) : IFileServiceUtils
{
    public async Task<EncryptedDirectory?> GetDirectory(string path, bool includeFilesAndDirs, int? groupId)
    {
        var user = await userService.GetCurrentUser();
        var pathHash = encryptionService.HashString(path);

        var directoryQueryable = context.Directories.Where(x => x.Owner == user);

        // Override check with groups instead of the user if groupId is provided
        if (groupId is not null)
        {
            directoryQueryable = from dir in context.Directories
                                 join groupRole in context.UserGroupRoles on dir.GroupId equals groupRole.UserGroupId
                                 where groupRole.UserId == user.Id && dir.GroupId == groupId
                                 select dir;
        }

        if (includeFilesAndDirs)
        {
            return await directoryQueryable
                .Include(dir => dir.Files)
                .ThenInclude(file => file.Tags)
                .Include(dir => dir.Directories)
                .FirstOrDefaultAsync(x => x.PathHash == pathHash);
        }

        return await directoryQueryable
            .FirstOrDefaultAsync(x => x.PathHash == pathHash);
    }

    public async Task<(EncryptedDirectory, EncryptedFile)> GetDirAndFile(string directoryPath, string name,
        int? groupId, bool includeTags = false)
    {
        var user = await userService.GetCurrentUser();
        var pathHash = encryptionService.HashString(directoryPath);

        var query = context.Directories
            .Where(x => x.Owner == user);

        if (groupId is not null)
        {
            query = from dir in context.Directories
                    join groupRole in context.UserGroupRoles on dir.GroupId equals groupRole.UserGroupId
                    where groupRole.UserId == user.Id && dir.GroupId == groupId
                    select dir;
        }

        query = query.Include(dir => dir.Files);

        if (includeTags)
        {
            query = query.Include(dir => dir.Files)
                .ThenInclude(file => file.Tags);
        }

        var directory = query.FirstOrDefault(x => x.PathHash == pathHash);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var nameHash = encryptionService.HashString(name);
        var file = directory.Files.FirstOrDefault(file => file.NameHash == nameHash);

        if (file is null)
        {
            throw new FileNotFoundException($"File with name ${name} not found in directory ${directoryPath}.");
        }

        return (directory, file);
    }
}
