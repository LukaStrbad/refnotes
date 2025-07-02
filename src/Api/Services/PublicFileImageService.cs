using Api.Jobs;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Api.Services;

public sealed class PublicFileImageService : IPublicFileImageService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileImageService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileService _fileService;

    public PublicFileImageService(ILogger<PublicFileImageService> logger, ISchedulerFactory schedulerFactory,
        RefNotesContext context, IFileStorageService fileStorageService, IFileService fileService)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _context = context;
        _fileStorageService = fileStorageService;
        _fileService = fileService;
    }

    public async Task ScheduleImageRefreshForPublicFile(int publicFileId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        _logger.LogInformation("Creating job to update images for public file {publicFileId}", publicFileId);

        var jobId = $"{UpdatePublicFileImagesJob.Name}-{publicFileId}";
        var jobKey = new JobKey(jobId);

        if (await scheduler.CheckExists(jobKey))
        {
            _logger.LogInformation("Deleting existing job {jobId}", jobId);
            await scheduler.Interrupt(jobKey);
            await scheduler.DeleteJob(jobKey);
        }

        var job = JobBuilder.Create<UpdatePublicFileImagesJob>()
            .WithIdentity(jobKey)
            .UsingJobData("publicFileId", publicFileId)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{UpdatePublicFileImagesJob.Name}-trigger-{publicFileId}")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(job, trigger);
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
            var file = await _fileService.GetEncryptedFileAsync(filePath, null);
            if (file is null)
                continue;

            var publicFileImage = new PublicFileImage(publicFile.Id, file.Id);
            await _context.PublicFileImages.AddAsync(publicFileImage);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RemoveImagesForPublicFile(int publicFileId)
    {
        _logger.LogInformation("Removing images for public file {publicFileId}", publicFileId);

        await _context.PublicFileImages.Where(image => image.PublicFileId == publicFileId).ExecuteDeleteAsync();
    }
}
