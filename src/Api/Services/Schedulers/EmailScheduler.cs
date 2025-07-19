using Api.Jobs;
using Api.Utils;
using Data.Model;
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

    private async Task ScheduleEmail<T>(string jobId, JobDataMap jobDataMap) where T : IJob
    {
        var job = JobBuilder.Create<T>()
            .WithIdentity(jobId)
            .UsingJobData(jobDataMap)
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
        _logger.LogInformation("Scheduled email send to {SendTo}", StringSanitizer.SanitizeLog(sendTo));

        var jobDataMap = new JobDataMap
        {
            { "sendTo", sendTo },
            { "name", name },
            { "token", token },
            { "lang", lang }
        };

        var guid = Guid.NewGuid().ToString();
        var jobId = $"{VerificationEmailJob.Name}-{guid}";
        await ScheduleEmail<VerificationEmailJob>(jobId, jobDataMap);
    }

    public async Task SchedulePasswordResetEmail(User user, string token, string lang)
    {
        _logger.LogInformation("Scheduled email send to {SendTo}", StringSanitizer.SanitizeLog(user.Email));
        var jobDataMap = new JobDataMap
        {
            { "sendTo", user.Email },
            { "name", user.Name },
            { "username", user.Username },
            { "token", token },
            { "lang", lang }
        };

        var guid = Guid.NewGuid().ToString();
        var jobId = $"{PasswordResetEmailJob.Name}-{guid}";
        await ScheduleEmail<PasswordResetEmailJob>(jobId, jobDataMap);
    }
}
