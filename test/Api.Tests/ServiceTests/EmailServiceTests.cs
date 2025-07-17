using Api.Model;
using Api.Services;
using Api.Tests.Extensions;
using MailKit.Net.Smtp;
using MimeKit;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public sealed class EmailServiceTests
{
    private readonly ISmtpClient _smtpClient = Substitute.For<ISmtpClient>();
    private readonly IEmailTemplateService _emailTemplateService = Substitute.For<IEmailTemplateService>();
    private const string EmailConfirmationBaseUrl = "https://example.com/confirm-email";
    private const string PasswordResetBaseUrl = "https://example.com/reset-password";

    private EmailService CreateSut()
    {
        var appSettings =
            AppSettingsExtensions.WithEmailSettings(new EmailSettings("smtp.example.com", "username", "password",
                "Sender"), EmailConfirmationBaseUrl, PasswordResetBaseUrl);
        return new EmailService(appSettings, _smtpClient, _emailTemplateService);
    }

    [Fact]
    public async Task SendVerificationEmail_SendsEmail()
    {
        const string sendTo = "test@mail.com";
        const string name = "Test User";
        const string token = "verification-token";
        const string lang = "en";
        const string template = "Name: {{name}} Link: {{confirmation_link}}";

        _emailTemplateService.GetTemplate(EmailType.ConfirmEmail, lang).Returns(("Title", template));

        var sut = CreateSut();
        await sut.SendVerificationEmail(sendTo, name, token, lang);

#pragma warning disable xUnit1051
        await _smtpClient.Received(1).SendAsync(Arg.Is<MimeMessage>(msg =>
            msg.To.Count == 1 &&
            msg.To[0].Name == name &&
            msg.Subject == "Title" &&
            msg.Body.ToString().Contains($"Name: {name}") &&
            msg.Body.ToString().Contains($"Link: {EmailConfirmationBaseUrl}/{token}")
        ));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task SendPasswordResetEmail_SendsEmail()
    {
        const string sendTo = "test@mail.com";
        const string name = "Test User";
        const string token = "reset-token";
        const string lang = "en";
        const string template = "Name: {{name}} Link: {{reset_link}}";

        _emailTemplateService.GetTemplate(EmailType.ResetPassword, lang).Returns(("Title", template));

        var sut = CreateSut();
        await sut.SendPasswordResetEmail(sendTo, name, token, lang);

#pragma warning disable xUnit1051
        await _smtpClient.Received(1).SendAsync(Arg.Is<MimeMessage>(msg =>
            msg.To.Count == 1 &&
            msg.To[0].Name == name &&
            msg.Subject == "Title" &&
            msg.Body.ToString().Contains($"Name: {name}") &&
            msg.Body.ToString().Contains($"Link: {PasswordResetBaseUrl}/{token}")
        ));
#pragma warning restore xUnit1051
    }
}
