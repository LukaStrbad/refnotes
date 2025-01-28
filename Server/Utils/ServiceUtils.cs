using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Services;

namespace Server.Utils;

public static class ServiceUtils
{
    public static async Task<EncryptedDirectory?> GetDirectory(User user, IEncryptionService encryptionService, RefNotesContext context, string path, bool includeFiles)
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
}