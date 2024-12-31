using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Model;

namespace Server.Services;

public interface IBrowserService
{
    /// <summary>
    /// List the contents of a directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="path">Path of the directory</param>
    /// <returns>Name of the requested directory, together with its contents</returns>
    Task<ResponseDirectory?> List(ClaimsPrincipal claimsPrincipal, string path = "/");

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

    /// <summary>
    /// Add a new file to the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory where the file will be added</param>
    /// <param name="name">Name of the file to be added</param>
    /// <returns>Filesystem name of the added file</returns>
    Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);

    /// <summary>
    /// Get the filesystem path of a file in the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file</param>
    /// <returns>Filesystem path of the file, or null if not found</returns>
    Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);

    /// <summary>
    /// Delete a file from the specified directory.
    /// </summary>
    /// <param name="claimsPrincipal">Owner of the directory</param>
    /// <param name="directoryPath">Path of the directory containing the file</param>
    /// <param name="name">Name of the file to be deleted</param>
    Task DeleteFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
}

public class BrowserService(
    RefNotesContext context,
    IEncryptionService encryptionService,
    AppConfiguration appConfiguration) : IBrowserService
{
    public async Task<ResponseDirectory?> List(ClaimsPrincipal claimsPrincipal, string path = "/")
    {
        var user = await GetUser(claimsPrincipal);

        // Ensure root directory exists
        if (path is "/")
        {
            var rootDir = await context.Directories
                .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptionService.EncryptAesStringBase64("/"));
            if (rootDir is null)
            {
                await AddDirectory(claimsPrincipal, "/");
            }
        }

        var directory = await GetDirectory(user, path);

        return directory?.Decrypt(encryptionService);
    }

    public async Task AddDirectory(ClaimsPrincipal claimsPrincipal, string path)
    {
        var user = await GetUser(claimsPrincipal);

        path = NormalizePath(path);
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);

        var existingDir = await context.Directories
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        if (path is "/")
        {
            context.Directories.Add(new EncryptedDirectory(encryptedPath, user));
            await context.SaveChangesAsync();
            return;
        }

        if (existingDir is not null)
        {
            throw new ArgumentException($"Directory at path '{path}' already exists");
        }

        var (baseDir, _) = SplitDirPathName(path);
        var newDirectory = new EncryptedDirectory(encryptedPath, user);
        var parentDir = await GetDirectory(user, baseDir);

        // Recursively create parent directories if they don't exist
        if (parentDir is null)
        {
            await AddDirectory(claimsPrincipal, baseDir);
            parentDir = await GetDirectory(user, baseDir);
        }

        if (parentDir is null)
        {
            throw new DirectoryNotFoundException($"Parent directory at path '{baseDir}' not found");
        }

        parentDir.Directories.Add(newDirectory);
        await context.SaveChangesAsync();
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
        var directory = await context.Directories
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
        context.Entry(directory).State = EntityState.Deleted;
        await context.SaveChangesAsync();
    }

    public async Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var directory = await GetDirectory(user, directoryPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path '{directoryPath}' not found");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        if (directory.Files.Any(x => x.Name == encryptedName))
        {
            throw new FileAlreadyExistsException($"File with name '{name}' already exists in directory '{directoryPath}'");
        }

        var encryptedFile = new EncryptedFile(GenerateFilesystemName(), encryptedName);
        directory.Files.Add(encryptedFile);
        await context.SaveChangesAsync();
        return encryptedFile.FilesystemName;
    }

    public async Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = context.Directories
            .Include(dir => dir.Files)
            .FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);
        return file?.FilesystemName;
    }

    public async Task DeleteFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = context.Directories
            .Include(dir => dir.Files)
            .FirstOrDefault(x => x.Owner == user && x.Path == encryptedPath);

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

        directory.Files.Remove(file);
        context.Entry(file).State = EntityState.Deleted;
        await context.SaveChangesAsync();
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

    private async Task<User> GetUser(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity?.Name is not { } name || name == "")
        {
            throw new NoNameException();
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return user;
    }

    private async Task<EncryptedDirectory?> GetDirectory(User user, string path)
    {
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        var directory = await context.Directories
            .Include(dir => dir.Files)
            .Include(dir => dir.Directories)
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        return directory;
    }

    private string GenerateFilesystemName()
    {
        for (var i = 0; i < 100; i++)
        {
            var randomName = Path.GetRandomFileName();
            var baseDir = appConfiguration.DataDir;
            var path = Path.Join(baseDir, randomName);
            if (File.Exists(path)) continue;
            return randomName;
        }

        throw new Exception("Failed to generate unique filesystem name.");
    }
}
