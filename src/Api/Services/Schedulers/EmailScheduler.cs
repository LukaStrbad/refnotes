using Api.Jobs;
using Quartz;

namespace Api.Services.Schedulers;

public sealed class EmailScheduler : IEmailScheduler
{
    private readonly ILogger<EmailScheduler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;

    public EmailScheduler(ILogger<EmailScheduler> logger, ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    private async Task ScheduleEmail<T>(string jobId, string sendTo, string name, string token, string lang) where T : IJob
    {
        var job = JobBuilder.Create<T>()
            .WithIdentity(jobId)
            .UsingJobData("sendTo", sendTo)
            .UsingJobData("name", name)
            .UsingJobData("token", token)
            .UsingJobData("lang", lang)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobId}-trigger")
            .StartNow()
            .Build();

        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ScheduleJob(job, trigger);
    }

    public async Task ScheduleVerificationEmail(string sendTo, string name, string token, string lang)
    {
        _logger.LogInformation("Scheduled email send to {SendTo}", sendTo);

        var guid = Guid.NewGuid().ToString();
        var jobId = $"{VerificationEmailJob.Name}-{guid}";
        await ScheduleEmail<VerificationEmailJob>(jobId, sendTo, name, token, lang);
    }
    
    public async Task SchedulePasswordResetEmail(string sendTo, string name, string token, string lang)
    {
        _logger.LogInformation("Scheduled email send to {SendTo}", sendTo);

        var guid = Guid.NewGuid().ToString();
        var jobId = $"{PasswordResetEmailJob.Name}-{guid}";
        await ScheduleEmail<PasswordResetEmailJob>(jobId, sendTo, name, token, lang);
    }
}
