using Api.Services;
using Quartz;

namespace Api.Jobs;

public class VerificationEmailJob : IJob
{
    public const string Name = "VerificationEmailJob";

    private readonly IEmailService _emailService;

    public VerificationEmailJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;

        var sendTo = dataMap.GetString("sendTo");
        if (sendTo is null)
            throw new Exception("SendTo property not found.");
        var token = dataMap.GetString("token");
        if (token is null)
            throw new Exception("Token property not found.");
        var name = dataMap.GetString("name");
        if (name is null)
            throw new Exception("Name property not found.");

        var lang = dataMap.GetString("lang") ?? "en";

        await _emailService.SendVerificationEmail(sendTo, name, token, lang);
    }
}
