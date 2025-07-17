using System.Text.RegularExpressions;
using Api.Model;

namespace Api.Services;

public partial class EmailTemplateService : IEmailTemplateService
{
    public (string Title, string Html) GetTemplate(EmailType type, string lang)
    {
        const string templatesFolder = "Templates/Email";
        var fileBaseName = type switch
        {
            EmailType.ConfirmEmail => "ConfirmEmail",
            EmailType.ResetPassword => "ResetPassword",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        var fileName = $"{fileBaseName}.{lang}.html";
        var fullPath = Path.Join(templatesFolder, fileName);
        if (!File.Exists(fullPath))
        {
            fileName = $"{fileBaseName}.html";
            fullPath = Path.Join(templatesFolder, fileName);
        }

        if (!File.Exists(fullPath))
            throw new Exception($"Email template doesn't exist for type: {type}");

        var fileContent = File.ReadAllText(fullPath);
        var title = TitleRegex().Match(fileContent).Groups[1].Value.Trim();

        return (title, fileContent);
    }

    [GeneratedRegex("<title>(.*?)</title>", RegexOptions.Singleline)]
    private static partial Regex TitleRegex();
}
