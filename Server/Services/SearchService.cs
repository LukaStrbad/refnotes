using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Server.Db;
using Server.Db.Model;
using Server.Extensions;
using Server.Model;
using Server.Utils;

namespace Server.Services;

public interface ISearchService
{
    /// <summary>
    /// Searches for files where the path or tags contain the specified search term.
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Enumerable of all files that match the options</returns>
    IAsyncEnumerable<FileSearchResultDto> SearchFiles(string searchTerm);
}

public sealed class SearchService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    ServiceUtils utils,
    IMemoryCache cache) : ISearchService
{
    private async IAsyncEnumerable<FileSearchResultDto> SearchFilesInDirectory(User user,
        IQueryable<EncryptedFile> filesQuery, EncryptedDirectory directory, string searchTerm)
    {
        var directoryPath = directory.DecryptedPath(encryptionService);
        var cacheKey = $"{nameof(SearchFilesInDirectory)}-{user.Id}-{directoryPath}-{searchTerm}";
        List<FileSearchResultDto> directoryFiles;

        if (cache.TryGetValue(cacheKey, out List<FileSearchResultDto>? cachedFiles) && cachedFiles is not null)
        {
            directoryFiles = cachedFiles;
        }
        else
        {
            directoryFiles = await filesQuery.Select(file => file.ToSearchResultDto(directoryPath, encryptionService))
                .ToListAsync();
            cache.Set(cacheKey, directoryFiles, TimeSpan.FromMinutes(1));
        }

        foreach (var file in directoryFiles.Where(file =>
                     file.Path.Contains(searchTerm) || file.Tags.Any(tag => tag.Contains(searchTerm))))
        {
            yield return file;
        }
    }

    public async IAsyncEnumerable<FileSearchResultDto> SearchFiles(string searchTerm)
    {
        var user = await utils.GetUser();

        var directories = await context.Directories
            .Where(dir => dir.OwnerId == user.Id)
            .ToListAsync();  

        foreach (var directory in directories)
        {
            var filesQuery = context.Files
                .Include(file => file.Tags)
                .Where(file => file.EncryptedDirectoryId == directory.Id)
                .OrderBy(file => file.Id);

            await foreach (var file in SearchFilesInDirectory(user, filesQuery, directory, searchTerm))
            {
                yield return file;
            }
        }
    }
}