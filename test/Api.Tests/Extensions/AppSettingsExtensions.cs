using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Api.Tests.Extensions;

public static class AppSettingsExtensions
{
    public static AppSettings CreateWithValues(IEnumerable<KeyValuePair<string, string?>> values)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var logger = Substitute.For<ILogger<AppSettings>>();
        var appSettings = new AppSettings(config, logger);
        appSettings.ReloadConfig();
        return appSettings;
    }

    private static List<KeyValuePair<string, string?>> GetDefaultValues() =>
    [
        new("AppDomain", "localhost"),
        new("CorsOrigin", "http://localhost:4200"),
        new("AccessTokenExpiry", "5m")
    ];

    public static AppSettings WithEmailSettings(EmailSettings emailSettings, 
        string emailConfirmationBaseUrl,
        string passwordResetBaseUrl)
    {
        var defaultValues = GetDefaultValues();
        var values = new List<KeyValuePair<string, string?>>(defaultValues)
        {
            new("Email:Host", emailSettings.Host),
            new("Email:Username", emailSettings.Username),
            new("Email:Password", emailSettings.Password),
            new("Email:From", emailSettings.From),
            new("EmailConfirmationBaseUrl", emailConfirmationBaseUrl),
            new("PasswordResetBaseUrl", passwordResetBaseUrl)
        };
        return CreateWithValues(values);
    }
}
