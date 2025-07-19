namespace Api.Services;

public interface IEmailService
{
    Task SendVerificationEmail(string sendTo, string name, string token, string lang);

    Task SendPasswordResetEmail(string sendTo, string name, string username, string token, string lang);
}
