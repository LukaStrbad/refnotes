using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Services;

namespace Server.Utils;

public class ServiceUtils(RefNotesContext context, IEncryptionService encryptionService)
{
    public async Task<EncryptedDirectory?> GetDirectory(User user, string path, bool includeFiles)
    {
        var encryptedPath = encryptionService.EncryptAesStringBase64(path);
        if (includeFiles)
        {
            return await context.Directories
                .Include(dir => dir.Files)
                .ThenInclude(file => file.Tags)
                .Include(dir => dir.Directories)
                .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);
        }

        return await context.Directories
            .Include(dir => dir.Directories)
            .FirstOrDefaultAsync(x => x.Owner == user && x.Path == encryptedPath);
    }
    
    public async Task<User> GetUser(ClaimsPrincipal claimsPrincipal)
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
    
    public async Task<(EncryptedDirectory, EncryptedFile, User)> GetDirAndFile(ClaimsPrincipal claimsPrincipal,
        string directoryPath, string name, bool includeTags = false)
    {
        var user = await GetUser(claimsPrincipal);
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

        return (directory, file, user);
    }
}