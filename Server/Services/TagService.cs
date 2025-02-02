using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Utils;

namespace Server.Services;

public interface ITagService
{
    /// <summary>
    /// List all tags owned by the specified user.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the tags</param>
    /// <returns>Enumerable of all file tags</returns>
    public Task<List<string>> GetAllTags(ClaimsPrincipal claimsPrincipal);

    /// <summary>
    /// Add a tag to a file in the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory/file</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be added</param>
    Task AddFileTag(ClaimsPrincipal claimsPrincipal, string directoryPath, string name, string tag);

    /// <summary>
    /// Remove a tag from a file in the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory/file</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be removed</param>
    Task RemoveFileTag(ClaimsPrincipal claimsPrincipal, string directoryPath, string name, string tag);
}

public class TagService(RefNotesContext context, IEncryptionService encryptionService) : ITagService
{
    private readonly ServiceUtils _utils = new(context, encryptionService);

    public async Task<List<string>> GetAllTags(ClaimsPrincipal claimsPrincipal)
    {
        var user = await _utils.GetUser(claimsPrincipal);
        return await context.FileTags
            .Where(t => t.OwnerId == user.Id)
            .Select(t => t.DecryptedName(encryptionService))
            .ToListAsync();
    }

    public async Task AddFileTag(ClaimsPrincipal claimsPrincipal, string directoryPath, string name, string tag)
    {
        var (_, file, user) = await _utils.GetDirAndFile(claimsPrincipal, directoryPath, name, true);

        var encryptedTag = encryptionService.EncryptAesStringBase64(tag);
        if (file.Tags.Any(x => x.Name == encryptedTag))
        {
            // Do nothing if tag already exists
            return;
        }

        // Check if tag already exists
        // This is done to avoid creating duplicate tags and to improve search performance
        var existingTag = await context.FileTags
            .Where(t => t.OwnerId == user.Id)
            .FirstOrDefaultAsync(t => t.Name == encryptedTag);

        var tagToAdd = existingTag ?? new FileTag
        {
            Name = encryptedTag,
            Owner = user
        };

        file.Tags.Add(tagToAdd);
        await context.SaveChangesAsync();
    }

    public async Task RemoveFileTag(ClaimsPrincipal claimsPrincipal, string directoryPath, string name, string tag)
    {
        var (_, file, _) = await _utils.GetDirAndFile(claimsPrincipal, directoryPath, name, includeTags: true);

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