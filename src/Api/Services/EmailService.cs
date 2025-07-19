using MailKit.Net.Smtp;
using Api.Model;
using MimeKit;

namespace Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly AppSettings _appSettings;
    private readonly ISmtpClient _smtpClient;
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailService(AppSettings appSettings, ISmtpClient smtpClient, IEmailTemplateService emailTemplateService)
    {
        _appSettings = appSettings;
        _smtpClient = smtpClient;
        _emailTemplateService = emailTemplateService;
    }

    private async Task SendEmail(string sendTo, string name, string subject, string body, bool isHtml)
    {
        var emailSettings = _appSettings.EmailSettings;
        if (emailSettings is null)
            throw new Exception("Email settings are null");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("RefNotes", emailSettings.From));
        message.To.Add(new MailboxAddress(name, sendTo));
        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain")
        {
            Text = body
        };

        await _smtpClient.ConnectAsync(emailSettings.Host, 587, MailKit.Security.SecureSocketOptions.StartTls);
        await _smtpClient.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
        await _smtpClient.SendAsync(message);
        await _smtpClient.DisconnectAsync(true);
    }

    public async Task SendVerificationEmail(string sendTo, string name, string token, string lang)
    {
        var (title, html) = _emailTemplateService.GetTemplate(EmailType.ConfirmEmail, lang);
        var baseUrl = _appSettings.EmailConfirmationBaseUrl.TrimEnd('/');
        var confirmationLink = $"{baseUrl}/{token}";
        html = html.Replace("{{confirmation_link}}", confirmationLink);
        html = html.Replace("{{name}}", name);

        await SendEmail(sendTo, name, title, html, true);
    }

    public async Task SendPasswordResetEmail(string sendTo, string name, string username, string token, string lang)
    {
        var (title, html) = _emailTemplateService.GetTemplate(EmailType.ResetPassword, lang);
        var baseUrl = _appSettings.PasswordResetBaseUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/{token}?username={username}";
        html = html.Replace("{{reset_link}}", resetLink);
        html = html.Replace("{{name}}", name);
        
        await SendEmail(sendTo, name, title, html, true);
    }
}
