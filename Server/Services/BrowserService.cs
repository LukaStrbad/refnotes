using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Model;

namespace Server.Services;

public interface IBrowserService
{
    Task<ResponseDirectory?> List(ClaimsPrincipal claimsPrincipal, string path = "/");
    Task AddDirectory(ClaimsPrincipal claimsPrincipal, string path);
    Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
    Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
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
            // Special case for root directory
            if (existingDir is not null)
                return;
            
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

    public async Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = await GetUser(claimsPrincipal);
        var directory = await GetDirectory(user, directoryPath);

        if (directory is null)
        {
            throw new ArgumentException($"Directory at path '{directoryPath}' not found");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        if (directory.Files.Any(x => x.Name == encryptedName))
        {
            throw new ArgumentException($"File with name '{name}' already exists in directory '{directoryPath}'");
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
            throw new ArgumentException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);
        return file?.FilesystemName;
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