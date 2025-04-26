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
    IFileStorageService fileStorageService,
    ServiceUtils utils,
    IMemoryCache cache) : ISearchService
{
    private async Task<bool> ShouldIncludeInFullText(FileSearchResultDto file)
    {
        if (!FileUtils.IsTextFile(file.Path) && !FileUtils.IsMarkdownFile(file.Path))
            return false;

        var fileSize = await fileStorageService.GetFileSize(file.FilesystemName);
        return fileSize <= 1024 * 1024 * 10; // 10 MB
    }

    private async IAsyncEnumerable<FileSearchResultDto> SearchFilesInDirectory(User user,
        IQueryable<EncryptedFile> filesQuery, EncryptedDirectory directory, string searchTerm, List<string> tags,
        bool includeFullText)
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

        foreach (var file in directoryFiles.Where(file =>
                     tags.Count <= 0 ||
                     file.Tags.Any(tag => tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase))))
        {
            if (file.Path.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return file;
                continue;
            }

            if (!includeFullText || !await ShouldIncludeInFullText(file)) continue;

            string fileText;
            
            var fileCacheKey = $"{nameof(SearchFilesInDirectory)}-{file.FilesystemName}-{user.Id}";
            if (cache.TryGetValue(fileCacheKey, out string? cachedFileText) && cachedFileText is not null)
            {
                fileText = cachedFileText;
            }
            else
            {
                var fileContent = fileStorageService.GetFile(file.FilesystemName);
                using var sr = new StreamReader(fileContent);
                fileText = await sr.ReadToEndAsync();
                cache.Set(fileCacheKey, fileText, TimeSpan.FromMinutes(1));
            }

            if (fileText.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return file with { FoundByFullText = true };
            }
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
                               searchOptions.SearchTerm.ToLowerInvariant(), tags, searchOptions.IncludeFullText))
            {
                yield return file;
            }
        }
    }
}