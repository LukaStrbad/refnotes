using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class PublicFileService : IPublicFileService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileService> _logger;

    public PublicFileService(RefNotesContext context, ILogger<PublicFileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static string GenerateRandomHash() => Guid.NewGuid().ToString();

    public async Task<string?> GetUrlHashAsync(int encryptedFileId)
    {
        var file = await _context.PublicFiles
            .Where(file => file.EncryptedFileId == encryptedFileId)
            .Where(file => file.State == PublicFileState.Active)
            .FirstOrDefaultAsync();

        return file?.UrlHash;
    }

    public async Task<string> CreatePublicFileAsync(int encryptedFileId)
    {
        var encryptedFile = await _context.Files.FirstOrDefaultAsync(file => file.Id == encryptedFileId);
        if (encryptedFile is null)
        {
            _logger.LogError("File with ID {encryptedFileId} not found.", encryptedFileId);
            throw new FileNotFoundException();
        }

        // Check if the file already has a public file
        var publicFile = await _context.PublicFiles
            .Where(file => file.EncryptedFileId == encryptedFileId)
            .FirstOrDefaultAsync();
        if (publicFile is not null)
        {
            publicFile.State = PublicFileState.Active;
            await _context.SaveChangesAsync();
            return publicFile.UrlHash;
        }

        // Otherwise, generate a new hash
        var hash = GenerateRandomHash();

        // Create a new public file entry
        publicFile = new PublicFile(hash, encryptedFileId);
        _context.PublicFiles.Add(publicFile);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Public file created for file {encryptedFileId}.", encryptedFileId);
        return hash;
    }

    public async Task<bool> DeactivatePublicFileAsync(int fileId)
    {
        var file = await _context.PublicFiles.FirstOrDefaultAsync(file => file.EncryptedFileId == fileId);
        if (file is null)
        {
            _logger.LogError("Public file with ID {fileId} not found.", fileId);
            return false;
        }

        file.State = PublicFileState.Inactive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EncryptedFile?> GetEncryptedFileAsync(string urlHash)
    {
        var publicFile = await _context.PublicFiles.FirstOrDefaultAsync(file => file.UrlHash == urlHash);
        if (publicFile is not null)
            return await _context.Files.FirstOrDefaultAsync(file => file.Id == publicFile.EncryptedFileId);

        var sanitizedUrlHash = urlHash.Replace(Environment.NewLine, "").Replace("\r", "");
        _logger.LogError("Public file with hash {urlHash} not found.", sanitizedUrlHash);
        return null;
    }

    public async Task<bool> IsPublicFileActive(string urlHash)
    {
        var publicFile = await _context.PublicFiles.FirstOrDefaultAsync(file => file.UrlHash == urlHash);
        return publicFile?.State == PublicFileState.Active;
    }
}
