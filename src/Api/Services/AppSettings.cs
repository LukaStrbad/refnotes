using Api.Exceptions;
using Api.Utils;

namespace Api.Services;

public sealed class AppSettings
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppSettings> _logger;

    public bool CookieSecure { get; private set; }
    public TimeSpan AccessTokenExpiry { get; private set; }
    public string[] CorsOrigins { get; private set; } = [];
    public HashSet<string> AppDomains { get; private set; } = [];

    public AppSettings(IConfiguration configuration, ILogger<AppSettings> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void ReloadConfig()
    {
        lock (this)
        {
            CookieSecure = _configuration.GetValue("CookieSecure", false);

            var expirySetting = _configuration.GetValue<string>("AccessTokenExpiry");
            AccessTokenExpiry = TimeParser.ParseTimeString(expirySetting);
            if (AccessTokenExpiry == TimeSpan.Zero || AccessTokenExpiry < TimeSpan.Zero)
            {
                _logger.LogError("AccessTokenExpiry is set to zero or is less than zero, which is invalid");
                throw new InvalidConfigurationException("AccessTokenExpiry is set to zero, which is invalid");
            }

            CorsOrigins = GetCorsOrigins();
            AppDomains = GetAppDomains();
        }
    }

    private HashSet<string> GetAppDomains()
    {
        var appDomain = _configuration.GetValue<string?>("AppDomain");
        // Prioritize the single value AppDomain over a list of domains
        // Only use AppDomains if the single value is not set
        var appDomains = appDomain is not null ? [appDomain] : _configuration.GetSection("AppDomains").Get<string[]>();

        if (appDomains is null)
        {
            _logger.LogError("AppDomains not set in configuration");
            throw new InvalidConfigurationException("AppDomains not set in configuration");
        }

        if (appDomains.Length != 0) return new HashSet<string>(appDomains, StringComparer.OrdinalIgnoreCase);

        _logger.LogError("AppDomains list is empty");
        throw new InvalidConfigurationException("AppDomains list is empty");
    }

    private string[] GetCorsOrigins()
    {
        var corsOrigin = _configuration.GetValue<string>("CorsOrigin");
        var corsOrigins = corsOrigin is not null
            ? [corsOrigin]
            : _configuration.GetSection("CorsOrigins").Get<string[]>();

        if (corsOrigins is null)
        {
            _logger.LogError("CorsOrigins not set in configuration");
            throw new InvalidConfigurationException("CorsOrigins not set in configuration");
        }

        if (corsOrigins.Length != 0) return corsOrigins;

        _logger.LogError("CorsOrigins list is empty");
        throw new InvalidConfigurationException("CorsOrigins list is empty");
    }

    public static AppSettings Initialize(IServiceProvider serviceProvider)
    {
        var appSettings = new AppSettings(
            serviceProvider.GetRequiredService<IConfiguration>(),
            serviceProvider.GetRequiredService<ILogger<AppSettings>>()
        );
        appSettings.ReloadConfig();
        return appSettings;
    }
}
