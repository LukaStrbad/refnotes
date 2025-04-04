﻿using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Server.Db;
using Server.Db.Model;
using Server.Utils;

namespace Server.Services;

public interface ITagService
{
    /// <summary>
    /// List all tags owned by the specified user.
    /// </summary>
    /// <returns>List of all file tags</returns>
    public Task<List<string>> ListAllTags();

    /// <summary>
    /// List all tags associated with a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <returns>List of file tags</returns>
    public Task<List<string>> ListFileTags(string directoryPath, string name);

    /// <summary>
    /// Add a tag to a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be added</param>
    Task AddFileTag(string directoryPath, string name, string tag);

    /// <summary>
    /// Remove a tag from a file in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <param name="tag">Tag to be removed</param>
    Task RemoveFileTag(string directoryPath, string name, string tag);
}

public class TagService(RefNotesContext context, IEncryptionService encryptionService, ServiceUtils utils) : ITagService
{
    public async Task<List<string>> ListAllTags()
    {
        var user = await utils.GetUser();
        return await context.FileTags
            .Where(t => t.OwnerId == user.Id)
            .Select(t => t.DecryptedName(encryptionService))
            .ToListAsync();
    }

    public async Task<List<string>> ListFileTags(string directoryPath, string name)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, includeTags: true);
        return file.Tags.Select(t => t.DecryptedName(encryptionService)).ToList();
    }

    public async Task AddFileTag(string directoryPath, string name, string tag)
    {
        var user = await utils.GetUser();
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, includeTags: true);

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

        var tagToAdd = existingTag ?? new FileTag(encryptedTag, user.Id);

        file.Tags.Add(tagToAdd);
        await context.SaveChangesAsync();
    }

    public async Task RemoveFileTag(string directoryPath, string name, string tag)
    {
        var (_, file) = await utils.GetDirAndFile(directoryPath, name, includeTags: true);

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