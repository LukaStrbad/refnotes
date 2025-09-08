using Api.Services.Schedulers;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Files;

public sealed class PublicFileService : IPublicFileService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileService> _logger;
    private readonly IPublicFileImageService _publicFileImageService;
    private readonly IPublicFileScheduler _publicFileScheduler;

    public PublicFileService(RefNotesContext context, ILogger<PublicFileService> logger,
        IPublicFileImageService publicFileImageService, IPublicFileScheduler publicFileScheduler)
    {
        _context = context;
        _logger = logger;
        _publicFileImageService = publicFileImageService;
        _publicFileScheduler = publicFileScheduler;
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
            if (publicFile.State != PublicFileState.Active)
            {
                publicFile.State = PublicFileState.Active;
                await _context.SaveChangesAsync();

                await _publicFileScheduler.ScheduleImageRefreshForPublicFile(publicFile.Id);
            }

            return publicFile.UrlHash;
        }

        // Otherwise, generate a new hash
        var hash = GenerateRandomHash();

        // Create a new public file entry
        publicFile = new PublicFile(hash, encryptedFileId);
        _context.PublicFiles.Add(publicFile);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Public file created for file {encryptedFileId}.", encryptedFileId);

        // Schedule image refresh
        await _publicFileScheduler.ScheduleImageRefreshForPublicFile(publicFile.Id);

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

        // Remove images for the public file
        await _publicFileImageService.RemoveImagesForPublicFile(file.Id);

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

    public async Task<PublicFile?> GetPublicFileAsync(string urlHash)
    {
        return await _context.PublicFiles.FirstOrDefaultAsync(file => file.UrlHash == urlHash);
    }

    public async Task<bool> HasAccessToFileThroughHash(string urlHash, EncryptedFile file)
    {
        var publicFile = await GetPublicFileAsync(urlHash);
        if (publicFile is null)
            return false;

        return await _context.PublicFileImages
            .AnyAsync(image => image.PublicFileId == publicFile.Id && image.EncryptedFileId == file.Id);
    }
}
