using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class PublicFileImageService : IPublicFileImageService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileImageService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileService _fileService;

    public PublicFileImageService(ILogger<PublicFileImageService> logger,
        RefNotesContext context, IFileStorageService fileStorageService, IFileService fileService)
    {
        _logger = logger;
        _context = context;
        _fileStorageService = fileStorageService;
        _fileService = fileService;
    }

    public async Task UpdateImagesForPublicFile(int publicFileId)
    {
        var publicFile = await _context.PublicFiles.FindAsync(publicFileId);
        if (publicFile is null)
        {
            _logger.LogError("Public file with ID {publicFileId} not found.", publicFileId);
            return;
        }

        var encryptedFile = await _context.Files.FirstAsync(file => file.Id == publicFile.EncryptedFileId);
        await using var fileContent = _fileStorageService.GetFile(encryptedFile.FilesystemName);
        var dirOwner = await _fileService.GetDirOwnerAsync(encryptedFile);

        var rootFilePath = await _fileService.GetFilePathAsync(encryptedFile);
        var rootDirectory = FileUtils.NormalizePath(Path.GetDirectoryName(rootFilePath) ?? "/");

        // Delete all images for the public file
        await _context.PublicFileImages.Where(image => image.PublicFileId == publicFileId).ExecuteDeleteAsync();

        await foreach (var image in MarkdownUtils.GetImagesAsync(fileContent).Distinct())
        {
            // Skip in case the file is not an image
            if (!FileUtils.IsImageFile(image))
                continue;

            _logger.LogInformation("Adding image {image} to public file {publicFileId}", image, publicFileId);
            var filePath = FileUtils.ResolveRelativeFolderPath(rootDirectory, image);
            var file = await _fileService.GetEncryptedFileForOwnerAsync(filePath, dirOwner);

            if (file is null)
                continue;

            var publicFileImage = new PublicFileImage(publicFile.Id, file.Id);
            await _context.PublicFileImages.AddAsync(publicFileImage);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveImagesForEncryptedFile(int encryptedFileId)
    {
        var encryptedFile = await _context.Files.FindAsync(encryptedFileId);
        if (encryptedFile is null)
        {
            _logger.LogError("Encrypted file with ID {encryptedFileId} not found.", encryptedFileId);
            return;
        }

        var publicFile =
            await _context.PublicFiles.FirstOrDefaultAsync(file => file.EncryptedFileId == encryptedFileId);
        if (publicFile is not null)
            await RemoveImagesForPublicFile(publicFile.Id);
    }

    public async Task RemoveImagesForPublicFile(int publicFileId)
    {
        _logger.LogInformation("Removing images for public file {publicFileId}", publicFileId);

        await _context.PublicFileImages.Where(image => image.PublicFileId == publicFileId).ExecuteDeleteAsync();
    }
}
