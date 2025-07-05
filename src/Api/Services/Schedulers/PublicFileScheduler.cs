using Api.Jobs;
using Data;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Api.Services.Schedulers;

public sealed class PublicFileScheduler : IPublicFileScheduler
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PublicFileScheduler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;

    public PublicFileScheduler(RefNotesContext context, ILogger<PublicFileScheduler> logger, ISchedulerFactory schedulerFactory)
    {
        _context = context;
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleImageRefreshForEncryptedFile(int encryptedFileId)
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
            await ScheduleImageRefreshForPublicFile(publicFile.Id);
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
}
