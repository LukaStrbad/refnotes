using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server.Services;
using ServerTests.Extensions;

namespace ServerTests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ControllerFixture<T> : IDisposable where T : ControllerBase
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ServiceProvider _rootServiceProvider;
    
    public ControllerFixture()
    {
        var services = new ServiceCollection();

        services.AddScoped<IConfiguration>(
            implementationFactory: static _ => new ConfigurationManager());

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
        services.AddScopedSubstitute<HttpContext>();

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