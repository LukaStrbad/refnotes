using Api.Services;
using Api.Services.Schedulers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.Extensions;
using Api.Utils;

namespace Api.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ControllerFixture<T> : IDisposable where T : ControllerBase
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ServiceProvider _rootServiceProvider;

    public ControllerFixture()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>("AppDomain", "localhost"),
            new KeyValuePair<string, string?>("CorsOrigin", "http://localhost:4200"),
            new KeyValuePair<string, string?>("AccessTokenExpiry", "5m"),
            new KeyValuePair<string, string?>("JWT_PRIVATE_KEY", "1234567890")
        ]).Build();
        services.AddScoped<IConfiguration>(implementationFactory: _ => configuration);
        services.AddScoped(AppSettings.Initialize);

        services.AddScopedSubstitute<IBrowserService>();
        services.AddScopedSubstitute<IGroupPermissionService>();
        services.AddScopedSubstitute<IUserService>();
        services.AddScopedSubstitute<IAdminService>();
        services.AddScopedSubstitute<IAuthService>();
        services.AddScopedSubstitute<ISearchService>();
        services.AddScopedSubstitute<ITagService>();
        services.AddScopedSubstitute<IUserGroupService>();
        services.AddScopedSubstitute<IFileService>();
        services.AddScopedSubstitute<IFileStorageService>();
        services.AddScopedSubstitute<IPublicFileService>();
        services.AddScopedSubstitute<IAppDomainService>();
        services.AddScopedSubstitute<IPublicFileScheduler>();
        services.AddScopedSubstitute<IFavoriteService>();
        services.AddScopedSubstitute<IFileServiceUtils>();
        services.AddScopedSubstitute<IFileSyncService>();
        services.AddScopedSubstitute<IWebSocketFileSyncService>();
        services.AddScopedSubstitute<IEmailScheduler>();
        services.AddScopedSubstitute<IEmailConfirmService>();
        services.AddScopedSubstitute<IPasswordResetService>();
        services.AddScopedSubstitute<HttpContext>();
        services.AddLogging();

        // The controller
        services.AddTransient<T>();

        _rootServiceProvider = services.BuildServiceProvider(validateScopes: true);
    }

    public void Dispose()
    {
        _rootServiceProvider.Dispose();
        _disposables.ForEach(d => d.Dispose());
    }

    public IServiceProvider CreateServiceProvider()
    {
        var serviceScope = _rootServiceProvider.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        _disposables.Add(serviceProvider as IDisposable ?? throw new InvalidOperationException());
        _disposables.Add(serviceScope);
        return serviceProvider;
    }
}
