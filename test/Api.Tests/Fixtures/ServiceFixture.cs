using Api.Services;
using Api.Services.Redis;
using Api.Services.Schedulers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Api.Tests.Extensions;
using Api.Tests.Mocks;
using Api.Utils;
using Data;
using Medallion.Threading;
using NSubstitute;
using StackExchange.Redis;

namespace Api.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ServiceFixture<T> : IDisposable where T : class
{
    private readonly List<IDisposable> _disposables = [];
    private readonly ServiceCollection _baseServiceCollection;
    private ServiceProvider? _rootServiceProvider;

    public ServiceFixture()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>("AppDomain", "localhost"),
            new KeyValuePair<string, string?>("CorsOrigin", "http://localhost:4200"),
            new KeyValuePair<string, string?>("AccessTokenExpiry", "5m"),
            new KeyValuePair<string, string?>("JWT_PRIVATE_KEY", "1234567890")
        ]).Build();
        services.AddScoped<IConfiguration>(implementationFactory: _ => configuration);
        services.AddScoped(provider =>
        {
            var appSettings = AppSettings.Initialize(provider);
            Directory.CreateDirectory(appSettings.DataDir);
            return appSettings;
        });

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

        // Check if the service base type is already registered
        var serviceType = typeof(T);
        if (serviceType.BaseType is { } baseType)
        {
            // Remove the base type if it exists
            services.Remove(new ServiceDescriptor(baseType, serviceType));
        }

        // Remove the service type if it exists
        services.Remove(new ServiceDescriptor(serviceType, serviceType));

        // Register the service type as a scoped service
        services.AddScoped(serviceType);

        _baseServiceCollection = services;
    }

    public ServiceFixture<T> WithDb(TestDatabaseFixture dbFixture)
    {
        _baseServiceCollection.AddScoped<RefNotesContext>(implementationFactory: _ => dbFixture.CreateContext());
        return this;
    }

    public ServiceFixture<T> WithRedis()
    {
        _baseServiceCollection.AddScopedSubstitute<IConnectionMultiplexer>();
        _baseServiceCollection.AddScopedSubstitute<IRedisLockProvider>();
        _baseServiceCollection.AddScoped<IDatabase>(implementationFactory: services =>
        {
            var muxer = services.GetRequiredService<IConnectionMultiplexer>();
            var redis = Substitute.For<IDatabase>();
            muxer.GetDatabase().Returns(redis);
            return redis;
        });
        return this;
    }

    public ServiceFixture<T> ReplaceType<TBase, TConcrete>()
        where TBase : class
        where TConcrete : class, TBase
    {
        // Remove the base type if it exists
        _baseServiceCollection.Remove(new ServiceDescriptor(typeof(TBase), typeof(TConcrete)));
        // Register the concrete type as a scoped service
        _baseServiceCollection.AddScoped<TBase, TConcrete>();
        return this;
    }

    public ServiceFixture<T> WithFakeEncryption() => ReplaceType<IEncryptionService, FakeEncryptionService>();

    public void Dispose()
    {
        _rootServiceProvider?.Dispose();
        _disposables.ForEach(d => d.Dispose());
    }

    public IServiceProvider CreateServiceProvider()
    {
        // Create the root service provider if it doesn't exist
        _rootServiceProvider ??= _baseServiceCollection.BuildServiceProvider(validateScopes: true);

        var serviceScope = _rootServiceProvider.CreateScope();
        var serviceProvider = serviceScope.ServiceProvider;
        _disposables.Add(serviceProvider as IDisposable ?? throw new InvalidOperationException());
        _disposables.Add(serviceScope);
        return serviceProvider;
    }
}
