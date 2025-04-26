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
    /// <param name="searchOptions">Search options</param>
    /// <returns>Enumerable of all files that match the options</returns>
    IAsyncEnumerable<FileSearchResultDto> SearchFiles(SearchOptionsDto searchOptions);
}

public sealed class SearchService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    ServiceUtils utils,
    IMemoryCache cache) : ISearchService
{
    private async IAsyncEnumerable<FileSearchResultDto> SearchFilesInDirectory(User user,
        IQueryable<EncryptedFile> filesQuery, EncryptedDirectory directory, string searchTerm, List<string> tags)
    {
        var directoryPath = directory.DecryptedPath(encryptionService);
        List<FileSearchResultDto> directoryFiles;

        // Search term should not be included because searchTerm is only used after the directory files are fetched
        var cacheKey = $"{nameof(SearchFilesInDirectory)}-{directoryPath}-{user.Id}";

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

        var filesEnumerable = directoryFiles.Where(file => file.Path.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase));

        // Only filter by tags if there are any tags to filter by
        if (tags.Count > 0)
        {
            // Match the full string, not just a substring
            filesEnumerable = filesEnumerable.Where(file => file.Tags.Any(tag => tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase)));
        }

        foreach (var file in filesEnumerable)
        {
            yield return file;
        }
    }

    public async IAsyncEnumerable<FileSearchResultDto> SearchFiles(SearchOptionsDto searchOptions)
    {
        var user = await utils.GetUser();

        var directories = await context.Directories
            .Where(dir => dir.OwnerId == user.Id)
            .ToListAsync();

        var tags = searchOptions.Tags ?? [];
        tags = tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();

        foreach (var directory in directories)
        {
            var filesQuery = context.Files
                .Include(file => file.Tags)
                .Where(file => file.EncryptedDirectoryId == directory.Id)
                .OrderBy(file => file.Id);

            await foreach (var file in SearchFilesInDirectory(user, filesQuery, directory,
                               searchOptions.SearchTerm.ToLowerInvariant(), tags))
            {
                yield return file;
            }
        }
    }
}