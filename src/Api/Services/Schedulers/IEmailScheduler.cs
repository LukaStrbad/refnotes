namespace Api.Services.Schedulers;

public interface IEmailScheduler
{
    Task ScheduleVerificationEmail(string sendTo, string name, string token, string lang);

    Task SchedulePasswordResetEmail(string sendTo, string name, string token, string lang);
}
