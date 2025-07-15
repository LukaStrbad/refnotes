namespace Api.Services;

public sealed class AppDomainService : IAppDomainService
{
    private readonly AppSettings _appSettings;

    public AppDomainService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    public bool IsAppDomain(string domain)
    {
        return _appSettings.AppDomains.Contains(domain);
    }

    public bool IsUrlAppDomain(string url)
    {
        var uri = new Uri(url);
        return IsAppDomain(uri.Host);
    }
}
