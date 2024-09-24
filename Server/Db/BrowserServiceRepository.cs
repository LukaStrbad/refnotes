using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace Server.Db;

public class BrowserServiceRepository(RefNotesContext db, IEncryptionService encryptionService, AppConfiguration appConfig) : IBrowserServiceRepository
{
    public async Task<ResponseDirectory?> List(ClaimsPrincipal claimsPrincipal, string path = "/")
    {
        var user = GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        var directory = await db.Directories
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        return directory?.Decrypt(encryptionService);
    }

    public async Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = await db.Directories
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedFile = new EncryptedFile
        {
            FilesystemName = GenerateFilesystemName(),
            Name = encryptionService.EncryptAesStringBase64(name)
        };
        directory.Files.Add(encryptedFile);
        await db.SaveChangesAsync();

        return encryptedFile.FilesystemName;
    }

    public async Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name)
    {
        var user = GetUser(claimsPrincipal);
        var encryptedPath = encryptionService.EncryptAesStringBase64(directoryPath);
        var directory = await db.Directories
            .Include(dir => dir.Files)
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);

        if (directory is null)
        {
            throw new DirectoryNotFoundException($"Directory at path ${directoryPath} not found.");
        }

        var encryptedName = encryptionService.EncryptAesStringBase64(name);
        var file = directory.Files.FirstOrDefault(file => file.Name == encryptedName);
        return file?.FilesystemName;
    }

    private User GetUser(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity?.Name is not { } name || name.IsNullOrEmpty())
        {
            throw new NoNameException();
        }

        var user = db.Users.FirstOrDefault(x => x.Username == name);
        if (user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return user;
    }

    private string GenerateFilesystemName()
    {
        for (var i = 0; i < 100; i++)
        {
            var randomName = Path.GetRandomFileName();
            var baseDir = appConfig.DataDir;
            var path = Path.Join(baseDir, randomName);
            if (File.Exists(path)) continue;
            return randomName;
        }
        
        throw new Exception("Failed to generate unique filesystem name.");
    }
}