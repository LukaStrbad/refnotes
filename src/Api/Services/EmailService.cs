using System.Net;
using System.Net.Mail;
using System.Text;

namespace Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly AppSettings _appSettings;

    public EmailService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public async Task SendEmail(string to, string subject, string body)
    {
        var emailSettings = _appSettings.EmailSettings;
        if (emailSettings is null)
            throw new Exception("Email settings are null");

        var smtpClient = new SmtpClient(emailSettings.Host)
        {
            Port = 587,
            Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password),
            EnableSsl = true
        };

        var mail = new MailMessage(emailSettings.From, to)
        {
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8
        };

        await smtpClient.SendMailAsync(mail);
    }
}
