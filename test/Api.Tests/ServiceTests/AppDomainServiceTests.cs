using Api.Exceptions;
using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class AppDomainServiceTests
{
    private readonly ILogger<AppDomainService> _logger = Substitute.For<ILogger<AppDomainService>>();

    [Fact]
    public void AppDomainService_ConstructorThrows_ForEmptyConfig()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidConfigurationException>(() => new AppDomainService(configuration, _logger));
    }

    [Fact]
    public void IsAppDomain_ReturnsTrue_IfAppDomainIsSet()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration["AppDomain"] = "test";

        var appDomainService = new AppDomainService(configuration, _logger);

        var isAppDomain = appDomainService.IsAppDomain("test");
        Assert.True(isAppDomain);
    }

    [Fact]
    public void IsAppDomain_ReturnsFalse_IfAppDomainIsNotSet()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration["AppDomain"] = "test";

        var appDomainService = new AppDomainService(configuration, _logger);

        var isAppDomain = appDomainService.IsAppDomain("test2");
        Assert.False(isAppDomain);
    }

    [Fact]
    public void IsAppDomain_ReturnsTrue_IfAppDomainsListIsSet()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration["AppDomains:0"] = "test";
        configuration["AppDomains:1"] = "test2";

        var appDomainService = new AppDomainService(configuration, _logger);

        Assert.True(appDomainService.IsAppDomain("test"));
        Assert.True(appDomainService.IsAppDomain("test2"));
        Assert.False(appDomainService.IsAppDomain("test3"));
    }

    [Fact]
    public void IsUrlAppDomain_ReturnsTrue_IfAppDomainIsSet()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        configuration["AppDomain"] = "test.com";

        var appDomainService = new AppDomainService(configuration, _logger);

        var isAppDomain = appDomainService.IsUrlAppDomain("https://test.com");
        Assert.True(isAppDomain);
    }
}
