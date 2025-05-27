using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;

namespace Server.Services;

public sealed class PublicFileService : IPublicFileService
{
    private readonly RefNotesContext _context;

    public PublicFileService(RefNotesContext context)
    {
        _context = context;
    }

    private static string GenerateRandomHash() => Guid.NewGuid().ToString();

    public async Task<string?> GetUrlHash(int fileId)
    {
        var file = await _context.PublicFiles.FirstOrDefaultAsync(file => file.EncryptedFileId == fileId);
        return file?.UrlHash;
    }

    public async Task<string> CreatePublicFile(int fileId)
    {
        var encryptedFile = await _context.Files.FirstOrDefaultAsync(file => file.Id == fileId);
        if (encryptedFile is null)
        {
            throw new FileNotFoundException();
        }

        // Check if hash already exists
        var existingHash = await GetUrlHash(fileId);
        if (existingHash is not null)
            return existingHash;

        // Otherwise, generate a new hash
        var hash = GenerateRandomHash();

        // Create a new public file entry
        var publicFile = new PublicFile
        {
            UrlHash = hash,
            EncryptedFileId = fileId
        };
        _context.PublicFiles.Add(publicFile);
        await _context.SaveChangesAsync();
        return hash;
    }
    
    public async Task<bool> DeletePublicFile(int fileId)
    {
        var file = await _context.PublicFiles.FirstOrDefaultAsync(file => file.EncryptedFileId == fileId);
        if (file is null)
        {
            return false;
        }
        
        _context.PublicFiles.Remove(file);
        await _context.SaveChangesAsync();
        return true;
    }
}