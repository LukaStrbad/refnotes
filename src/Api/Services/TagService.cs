using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Api.Extensions;

namespace Api.Services;

public interface ITagService
{
    /// <summary>
    /// List all tags owned by the specified user.
    /// </summary>
    /// <returns>List of all file tags</returns>
    public Task<List<string>> ListAllTags();

    /// <summary>
    /// List all tags owned by the specified group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <returns>List of all file tags</returns>
    public Task<List<string>> ListAllGroupTags(int groupId);

    /// <summary>
    /// List all tags associated with a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <returns>List of file tags</returns>
    public Task<List<string>> ListFileTags(string directoryPath, string name, int? groupId);

    /// <summary>
    /// Add a tag to a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be added</param>
    Task AddFileTag(string directoryPath, string name, string tag, int? groupId);

    /// <summary>
    /// Remove a tag from a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be removed</param>
    Task RemoveFileTag(string directoryPath, string name, string tag, int? groupId);
}

public class TagService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IFileServiceUtils utils,
    IUserService userService) : ITagService
{
    public async Task<List<string>> ListAllTags()
    {
        var user = await userService.GetUser();
        return await context.FileTags
            .Where(t => t.OwnerId == user.Id)
            .Select(t => t.DecryptedName(encryptionService))
            .ToListAsync();
    }

    public async Task<List<string>> ListAllGroupTags(int groupId)
    {
        return await context.FileTags
            .Where(t => t.GroupOwnerId == groupId)
            .Select(t => t.DecryptedName(encryptionService))
            .ToListAsync();
    }

    public async Task<List<string>> ListFileTags(string directoryPath, string name, int? groupId)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId, includeTags: true);
        return file.Tags.Select(t => t.DecryptedName(encryptionService)).ToList();
    }

    public async Task AddFileTag(string directoryPath, string name, string tag, int? groupId)
    {
        var user = await userService.GetUser();
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId, includeTags: true);

        var encryptedTag = encryptionService.EncryptAesStringBase64(tag);
        if (file.Tags.Any(x => x.Name == encryptedTag))
        {
            // Do nothing if tag already exists
            return;
        }

        // Check if tag already exists
        FileTag? existingTag;
        if (groupId is null)
        {
            existingTag = await context.FileTags
                .Where(t => t.OwnerId == user.Id)
                .FirstOrDefaultAsync(t => t.Name == encryptedTag);
        }
        else
        {
            existingTag = await context.FileTags
                .Where(t => t.GroupOwnerId == groupId)
                .FirstOrDefaultAsync(t => t.Name == encryptedTag);
        }
        
        var tagToAdd = existingTag ?? new FileTag
        {
            Name = encryptedTag,
            Owner = user
        };

        file.Tags.Add(tagToAdd);
        await context.SaveChangesAsync();
    }

    public async Task RemoveFileTag(string directoryPath, string name, string tag, int? groupId)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, groupId, includeTags: true);

        var encryptedTag = encryptionService.EncryptAesStringBase64(tag);
        var tagToRemove = file.Tags.FirstOrDefault(x => x.Name == encryptedTag);
        if (tagToRemove is null)
        {
            // Do nothing if tag does not exist
            return;
        }

        tagToRemove.Files.Remove(file);
        // Delete tag if it is no longer associated with any files
        if (tagToRemove.Files.Count == 0)
        {
            context.Entry(tagToRemove).State = EntityState.Deleted;
        }

        await context.SaveChangesAsync();
    }
}
