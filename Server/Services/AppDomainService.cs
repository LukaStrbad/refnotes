using Server.Exceptions;

namespace Server.Services;

public sealed class AppDomainService : IAppDomainService
{
    private readonly HashSet<string> _appDomains;
    
    public AppDomainService(IConfiguration configuration, ILogger<AppDomainService> logger)
    {
        var appDomain = configuration.GetValue<string?>("AppDomain");
        // Prioritize the single value AppDomain over a list of domains
        // Only use AppDomains if the single value is not set
        var appDomains = appDomain is not null ? [appDomain] : configuration.GetSection("AppDomains").Get<string[]>();

        if (appDomains is null)
        {
            logger.LogError("AppDomains not set in configuration");
            throw new InvalidConfigurationException("AppDomains not set in configuration");
        }

        if (appDomains.Length == 0)
        {
            logger.LogError("AppDomains list is empty");
            throw new InvalidConfigurationException("AppDomains list is empty");
        }
        
        _appDomains = new HashSet<string>(appDomains);
    }
    
    public bool IsAppDomain(string domain)
    {
        return _appDomains.Contains(domain);
    }

    public bool IsUrlAppDomain(string url)
    {
        var uri = new Uri(url);
        return IsAppDomain(uri.Host);
    }
}