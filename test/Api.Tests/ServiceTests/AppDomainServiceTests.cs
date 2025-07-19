using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class AppDomainServiceTests
{
    private static AppSettings CreateAppSettings(List<KeyValuePair<string, string?>> config)
    {
        // Add default values to the configuration
        config.Add(new KeyValuePair<string, string?>("CorsOrigin", "http://localhost"));
        config.Add(new KeyValuePair<string, string?>("AccessTokenExpiry", "5m"));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        var appSettings = new AppSettings(configuration, Substitute.For<ILogger<AppSettings>>());
        appSettings.ReloadConfig();
        return appSettings;
    }

    [Fact]
    public void IsAppDomain_ReturnsTrue_IfAppDomainIsSet()
    {
        var appDomainService =
            new AppDomainService(CreateAppSettings([new KeyValuePair<string, string?>("AppDomain", "test")]));

        var isAppDomain = appDomainService.IsAppDomain("test");
        Assert.True(isAppDomain);
    }

    [Fact]
    public void IsAppDomain_ReturnsFalse_IfAppDomainIsNotSet()
    {
        var appDomainService =
            new AppDomainService(CreateAppSettings([new KeyValuePair<string, string?>("AppDomain", "test")]));

        var isAppDomain = appDomainService.IsAppDomain("test2");
        Assert.False(isAppDomain);
    }

    [Fact]
    public void IsAppDomain_ReturnsTrue_IfAppDomainsListIsSet()
    {
        var appDomainService = new AppDomainService(CreateAppSettings([
            new KeyValuePair<string, string?>("AppDomains:0", "test"),
            new KeyValuePair<string, string?>("AppDomains:1", "test2")
        ]));

        Assert.True(appDomainService.IsAppDomain("test"));
        Assert.True(appDomainService.IsAppDomain("test2"));
        Assert.False(appDomainService.IsAppDomain("test3"));
    }

    [Fact]
    public void IsUrlAppDomain_ReturnsTrue_IfAppDomainIsSet()
    {
        var appDomainService = new AppDomainService(CreateAppSettings([
            new KeyValuePair<string, string?>("AppDomain", "test.com")
        ]));

        var isAppDomain = appDomainService.IsUrlAppDomain("https://test.com");
        Assert.True(isAppDomain);
    }
}
