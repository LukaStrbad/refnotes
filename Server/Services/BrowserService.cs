using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Utils;

namespace Server.Services;

public interface IBrowserService
{
    /// <summary>
    /// List the contents of a directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="path">Path of the directory</param>
    /// <returns>Name of the requested directory, together with its contents</returns>
    Task<DirectoryDto?> List(ClaimsPrincipal claimsPrincipal, string path = "/");

    /// <summary>
    /// Add a new directory at the specified path.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="path">Path where the new directory will be added</param>
    Task AddDirectory(ClaimsPrincipal claimsPrincipal, string path);

    /// <summary>
    /// Delete the directory at the specified path.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="path">Path of the directory to be deleted</param>
    Task DeleteDirectory(ClaimsPrincipal claimsPrincipal, string path);
}

public class BrowserService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    AppConfiguration appConfiguration) : BaseService(context), IBrowserService
{
    public async Task<DirectoryDto?> List(ClaimsPrincipal claimsPrincipal, string path = "/")
    {
        var user = await GetUser(claimsPrincipal);

        // Ensure root directory exists
        if (path is "/")
        {
            var rootDir = await Context.Directories
                .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptionService.EncryptAesStringBase64("/"));
            if (rootDir is null)
            {
                await AddDirectory(claimsPrincipal, "/");
            }
        }

        var directory = await ServiceUtils.GetDirectory(user, encryptionService, Context, path, true);

        return directory?.Decrypt(encryptionService);
    }

    public async Task AddDirectory(ClaimsPrincipal claimsPrincipal, string path)
    {
        var user = await GetUser(claimsPrincipal);

        path = NormalizePath(path);
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);

        var existingDir = await Context.Directories
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        if (path is "/")
        {
            Context.Directories.Add(new EncryptedDirectory(encryptedPath, user));
            await Context.SaveChangesAsync();
            return;
        }

        if (existingDir is not null)
        {
            throw new ArgumentException($"Directory at path '{path}' already exists");
        }

        var (baseDir, _) = SplitDirPathName(path);
        var newDirectory = new EncryptedDirectory(encryptedPath, user);
        var parentDir = await ServiceUtils.GetDirectory(user, encryptionService, Context, baseDir, false);

        // Recursively create parent directories if they don't exist
        if (parentDir is null)
        {
            await AddDirectory(claimsPrincipal, baseDir);
            parentDir = await ServiceUtils.GetDirectory(user, encryptionService, Context, baseDir, false);
        }

        if (parentDir is null)
        {
            throw new DirectoryNotFoundException($"Parent directory at path '{baseDir}' not found");
        }

        parentDir.Directories.Add(newDirectory);
        await Context.SaveChangesAsync();
    }

    public async Task DeleteDirectory(ClaimsPrincipal claimsPrincipal, string path)
    {
        var user = await GetUser(claimsPrincipal);

        path = NormalizePath(path);

        if (path is "/")
        {
            throw new ArgumentException("Cannot delete root directory");
        }

        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        var directory = await Context.Directories
            .Include(dir => dir.Parent)
            .Include(dir => dir.Files)
            .Include(dir => dir.Directories)
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

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
        Context.Entry(directory).State = EntityState.Deleted;
        await Context.SaveChangesAsync();
    }

    private record DirPathName(string ParentDir, string Name);

    private static DirPathName SplitDirPathName(string path)
    {
        var parentDir = Path.GetDirectoryName(path)?.Replace("\\", "/");
        if (string.IsNullOrWhiteSpace(parentDir))
            parentDir = "/";
        var name = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Path must not be root");
        return new DirPathName(parentDir, name);
    }

    private static string NormalizePath(string path) => path.Replace("\\", "/");
}