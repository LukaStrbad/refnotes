using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Services;

namespace Server.Utils;

public class ServiceUtils(
    RefNotesContext context,
    IEncryptionService encryptionService,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor)
{
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public async Task<EncryptedDirectory?> GetDirectory(string path, bool includeFiles)
    {
        var user = await GetUser();
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        if (includeFiles)
        {
            return await context.Directories
                .Include(dir => dir.Files)
                .ThenInclude(file => file.Tags)
                .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);
        }

        return await context.Directories
            .Include(dir => dir.Directories)
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);
    }

    public async Task<User> GetUser()
    {
        var claimsPrincipal = httpContextAccessor.HttpContext?.User;
        if (claimsPrincipal?.Identity?.Name is not { } name || name == "")
        {
            throw new NoNameException();
        }

        if (!claimsPrincipal.Identity.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        var cacheKey = $"user-{name}";

        var user = await cache.GetOrCreateAsync(cacheKey,
            _ => context.Users.FirstOrDefaultAsync(u => u.Username == name), _cacheOptions);

        if (user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return user;
    }

    public async Task<(EncryptedDirectory, EncryptedFile)> GetDirAndFile(string directoryPath, string name,
        bool includeTags = false)
    {
        var user = await GetUser();
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var query = context.Directories
            .Include(dir => dir.Files)
            .AsQueryable();
        if (includeTags)
        {
            query = query.Include(dir => dir.Files)
                .ThenInclude(file => file.Tags);
        }

        var directory = query.FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);

        if (file is null)
        {
            throw new FileNotFoundException($"File with name ${name} not found in directory ${directoryPath}.");
        }

        return (directory, file);
    }

    /// <summary>
    /// Gets directory path from the given path with slashes as separators
    /// </summary>
    /// <param name="path">File or directory path</param>
    /// <returns></returns>
    private static string GetDirectoryPath(string path) => Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "/";

    public static (string, string) SplitDirAndFile(string path)
    {
        var directoryName = GetDirectoryPath(path);
        var fileName = Path.GetFileName(path);
        return (directoryName, fileName);
    }
}