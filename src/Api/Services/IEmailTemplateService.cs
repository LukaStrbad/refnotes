using Api.Model;

namespace Api.Services;

public interface IEmailTemplateService
{
    (string Title, string Html) GetTemplate(EmailType type, string lang);
}
