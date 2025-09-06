using System.Text.Json;
using Api.Model;
using Api.Utils;
using Data;
using Microsoft.EntityFrameworkCore;
using Data.Model;
using Api.Extensions;
using Api.Services.Files;
using StackExchange.Redis;

namespace Api.Services;

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
    IConnectionMultiplexer muxer,
    IUserService userService) : ISearchService
{
    private async Task<bool> ShouldIncludeInFullText(FileSearchResultInternal file)
    {
        if (!FileUtils.IsTextFile(file.Path) && !FileUtils.IsMarkdownFile(file.Path))
            return false;

        var fileSize = await fileStorageService.GetFileSize(file.FilesystemName);
        return fileSize <= 1024 * 1024 * 10; // 10 MB
    }

    private async IAsyncEnumerable<FileSearchResultDto> SearchFilesInDirectory(User user,
        IQueryable<EncryptedFile> filesQuery, EncryptedDirectory directory, string searchTerm, bool includeFullText,
        Func<IEnumerable<FileSearchResultInternal>, IEnumerable<FileSearchResultInternal>> filesFilter)
    {
        var redis = muxer.GetDatabase();
        var directoryPath = directory.DecryptedPath(encryptionService);
        List<FileSearchResultInternal> directoryFiles;

        // Search term should not be included because searchTerm is only used after the directory files are fetched
        var cacheKey = $"{nameof(SearchFilesInDirectory)}-{directoryPath}-{user.Id}";
        
        var cacheValue = await redis.StringGetAsync(cacheKey);
        if (cacheValue.HasValue)
        {
            directoryFiles = JsonSerializer.Deserialize<List<FileSearchResultInternal>>(cacheValue.ToString()) ?? [];
        }
        else
        {
            directoryFiles = await filesQuery.Select(file => file.ToSearchResultDto(directoryPath, encryptionService).ToInternal())
                .ToListAsync();
            await redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(directoryFiles), TimeSpan.FromMinutes(1));
        }

        foreach (var file in filesFilter(directoryFiles))
        {
            if (file.Path.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return file.ToDto();
                continue;
            }

            if (!includeFullText || !await ShouldIncludeInFullText(file)) continue;

            string fileText;

            var fileCacheKey = $"{nameof(SearchFilesInDirectory)}-{file.FilesystemName}-{user.Id}";
            cacheValue = await redis.StringGetAsync(fileCacheKey);
            if (cacheValue.HasValue)
            {
                fileText = encryptionService.DecryptAesStringBase64(cacheValue.ToString());
            }
            else
            {
                var fileContent = fileStorageService.GetFile(file.FilesystemName);
                using var sr = new StreamReader(fileContent);
                fileText = await sr.ReadToEndAsync();
                // Store data encrypted
                await redis.StringSetAsync(fileCacheKey, encryptionService.EncryptAesStringBase64(fileText), TimeSpan.FromMinutes(1));
            }

            if (fileText.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
            {
                yield return (file with { FoundByFullText = true }).ToDto();
            }
        }
    }

    public async IAsyncEnumerable<FileSearchResultDto> SearchFiles(SearchOptionsDto searchOptions)
    {
        var user = await userService.GetCurrentUser();

        var directories = await context.Directories
            .Where(dir => dir.OwnerId == user.Id)
            .ToListAsync();

        // Filter directories by starting path
        directories = directories.Where(dir =>
                dir.DecryptedPath(encryptionService).StartsWith(searchOptions.DirectoryPath))
            .ToList();

        var filesFilter = GetFilesFilter(searchOptions);

        foreach (var directory in directories)
        {
            var filesQuery = context.Files
                .Include(file => file.Tags)
                .Where(file => file.EncryptedDirectoryId == directory.Id)
                .OrderBy(file => file.Id);

            await foreach (var file in SearchFilesInDirectory(user, filesQuery, directory,
                               searchOptions.SearchTerm.ToLowerInvariant(), searchOptions.IncludeFullText, filesFilter))
            {
                yield return file;
            }
        }
    }

    private static Func<IEnumerable<FileSearchResultInternal>, IEnumerable<FileSearchResultInternal>> GetFilesFilter(
        SearchOptionsDto searchOptions)
    {
        var tags = searchOptions.Tags ?? [];
        tags = tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();

        var fileTypes = searchOptions.FileTypes ?? [];
        fileTypes = fileTypes
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Select(type => type.TrimStart('.')).ToList();

        var modifiedFrom = searchOptions.ModifiedFrom ?? DateTime.MinValue;
        var modifiedTo = searchOptions.ModifiedTo ?? DateTime.MaxValue;

        return FilterFunc;

        IEnumerable<FileSearchResultInternal> FilterFunc(IEnumerable<FileSearchResultInternal> files)
        {
            var filesEnumerable = files;

            if (tags.Count > 0)
            {
                filesEnumerable = filesEnumerable.Where(file =>
                    file.Tags.Any(tag => tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase)));
            }

            if (searchOptions.ModifiedFrom is not null || searchOptions.ModifiedTo is not null)
            {
                filesEnumerable =
                    filesEnumerable.Where(file => file.Modified >= modifiedFrom && file.Modified <= modifiedTo);
            }

            if (fileTypes.Count > 0)
            {
                filesEnumerable = filesEnumerable.Where(file =>
                    fileTypes.Contains(Path.GetExtension(file.Path)[1..], StringComparer.InvariantCultureIgnoreCase));
            }

            return filesEnumerable;
        }
    }
}
