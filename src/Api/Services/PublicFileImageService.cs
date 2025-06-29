using Api.Jobs;
using Api.Utils;
using Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Api.Services;

public sealed class PublicFileImageService : IPublicFileImageService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileImageService> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IFileStorageService _fileStorageService;

    public PublicFileImageService(ILogger<PublicFileImageService> logger, ISchedulerFactory schedulerFactory,
        RefNotesContext context, IFileStorageService fileStorageService)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _context = context;
        _fileStorageService = fileStorageService;
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
        var fileContent = _fileStorageService.GetFile(encryptedFile.FilesystemName);

        await foreach (var image in MarkdownUtils.GetImagesAsync(fileContent))
        {
            _logger.LogInformation("Adding image {image} to public file {publicFileId}", image, publicFileId);
        }
    }

    public async Task RemoveImagesForPublicFile(int publicFileId)
    {
        _logger.LogInformation("Removing images for public file {publicFileId}", publicFileId);

        await _context.PublicFileImages.Where(image => image.PublicFileId == publicFileId).ExecuteDeleteAsync();
    }
}
