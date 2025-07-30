using System.Reflection;
using Api.Services;
using Api.Tests.Data.Attributes;
using Api.Tests.Fixtures;
using Api.Tests.Mocks;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Api.Services.Schedulers;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Api.Tests.Extensions;
using Bogus;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Quartz;
using StackExchange.Redis;

namespace Api.Tests.Data;

public sealed class AutoDataResolver : IAsyncDisposable
{
    private readonly MethodInfo _methodInfo;
    private readonly ParameterInfo[] _methodParameters;
    private readonly TestDatabaseFixture _testDatabaseFixture;
    private readonly RefNotesContext _context;
    private readonly string _testFolder;

    private IServiceProvider? _rootProvider;
    private IServiceScope? _serviceScope;
    private IServiceProvider? _serviceProvider;

    private static readonly List<Type> MockInterfaceList =
    [
        typeof(IUserService),
        typeof(IEncryptionService),
        typeof(IFileStorageService),
        typeof(IFileServiceUtils),
        typeof(IFileService),
        typeof(IUserGroupService),
        typeof(IBrowserService),
        typeof(IAppDomainService),
        typeof(ISchedulerFactory),
        typeof(IPublicFileImageService),
        typeof(IGroupPermissionService),
        typeof(IPublicFileService),
        typeof(IPublicFileScheduler),
        typeof(IFileSyncService),
        typeof(IWebSocketMessageHandler),
        typeof(ISmtpClient),
        typeof(IHttpContextAccessor)
    ];

    private readonly List<Type> _realizedMocks = [];
    private readonly List<User> _createdUsers = [];
    private readonly Dictionary<string, User> _paramNameToUser = [];

    public AutoDataResolver(MethodInfo methodInfo, TestDatabaseFixture testDatabaseFixture)
    {
        _methodInfo = methodInfo;
        _methodParameters = methodInfo.GetParameters();
        _testDatabaseFixture = testDatabaseFixture;
        _testFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testFolder);

        _context = _testDatabaseFixture.CreateContext();
    }

    public Task RegisterServicesAsync()
    {
        _serviceProvider = CreateServiceProvider();
        return Task.CompletedTask;
    }

    public async Task<object?[]> ResolveTestParameters()
    {
        if (_serviceProvider is null)
            throw new Exception("Service provider not initialized");

        var parameters = new List<object?>();

        foreach (var parameter in _methodParameters)
        {
            parameters.Add(await ResolveParameter(parameter));
        }

        if (_createdUsers.Count == 0)
            await CreateUser("test_user", [], null);

        return parameters.ToArray();
    }

    private async Task<object?> ResolveParameter(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        if (parameter.GetCustomAttribute<FixtureUserAttribute>() is { } fixtureUser)
        {
            var username = fixtureUser.Username ?? $"user_{RandomString(32)}";
            var roles = fixtureUser.Roles ?? [];
            return await CreateUser(username, roles, parameter.Name);
        }

        if (parameter.ParameterType == typeof(UserGroup))
        {
            var groupUser = _createdUsers.FirstOrDefault() ?? await CreateUser("test_user", [], null);
            string? groupName = null;

            // If FixtureGroupAttribute is present, use the user from the attribute
            if (parameter.GetCustomAttribute<FixtureGroupAttribute>() is not { } fixtureGroupAttribute)
                return await CreateRandomGroup(groupName, groupUser);

            if (fixtureGroupAttribute.ForUser is not null)
            {
                if (!_paramNameToUser.TryGetValue(fixtureGroupAttribute.ForUser, out groupUser))
                    throw new Exception($"User {fixtureGroupAttribute.ForUser} not found");
            }

            if (fixtureGroupAttribute.GroupName is not null)
            {
                groupName = fixtureGroupAttribute.GroupName;
            }

            var group = await CreateRandomGroup(groupName, groupUser);

            if (fixtureGroupAttribute.AddNull)
                return new AlternativeParameter(group, null);

            return group;
        }

        if (parameter.ParameterType.IsGenericType &&
            (parameter.ParameterType.GetGenericTypeDefinition() == typeof(Faker<>) ||
             parameter.ParameterType.GetGenericTypeDefinition() == typeof(DatabaseFaker<>)))
            return ResolveFaker(parameter.ParameterType);

        if (parameter.ParameterType.BaseType?.IsGenericType == true &&
            parameter.ParameterType.BaseType?.GetGenericTypeDefinition() == typeof(FakerImplementationBase<>))
            return ResolveFakerImpl(parameter.ParameterType);

        if (parameter.GetCustomAttribute<RandomStringAttribute>() is { } randomStringAttribute)
        {
            return randomStringAttribute.Prefix + RandomString(randomStringAttribute.Length);
        }

        var sutType = typeof(Sut<>);
        if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == sutType)
        {
            var genericType = parameter.ParameterType.GenericTypeArguments.First();
            var sutValue = _serviceProvider.GetRequiredService(genericType);
            return Activator.CreateInstance(parameter.ParameterType, sutValue, _context, _serviceProvider,
                _createdUsers);
        }

        return _serviceProvider.GetRequiredService(parameter.ParameterType);
    }

    private object CreateFaker(Type fakerType)
    {
        if (!fakerType.IsGenericType)
            throw new Exception("Faker type must be a generic type");

        object? faker;

        if (fakerType.GetGenericTypeDefinition() == typeof(Faker<>))
            faker = Activator.CreateInstance(fakerType);
        else if (fakerType.GetGenericTypeDefinition() == typeof(DatabaseFaker<>))
            faker = Activator.CreateInstance(fakerType, _context);
        else
            throw new Exception("Faker type must be a generic type");

        if (faker is null)
            throw new Exception("Faker instance cannot be null");

        return faker;
    }

    private Dictionary<Type, FakerImplementationBase> CreateFakerImplementations(Type parameterType,
        bool useDatabaseFaker = false)
    {
        var fakerImplementationBase = typeof(FakerImplementationBase<>);
        // Find all subclasses of FakerBase<>
        var fakerImplSubclassTypes = fakerImplementationBase.Assembly.GetTypes()
            .Where(t => t.BaseType?.IsGenericType == true &&
                        t.BaseType?.GetGenericTypeDefinition() == fakerImplementationBase)
            .ToList();

        var fakerImplementations = new Dictionary<Type, FakerImplementationBase>();
        foreach (var fakerImplSubclassType in fakerImplSubclassTypes)
        {
            var fakerImplGenericTypes = fakerImplSubclassType.BaseType?.GenericTypeArguments;
            if (fakerImplGenericTypes?.Length != 1)
                throw new Exception("Faker implementation must have exactly one generic type argument");

            // The type argument of the implementation class is the type of the model that the faker is for
            var modelType = fakerImplGenericTypes[0];

            object faker;
            if (useDatabaseFaker)
            {
                faker = Activator.CreateInstance(typeof(DatabaseFaker<>).MakeGenericType(modelType), _context) ??
                        throw new Exception("Faker instance cannot be null");
            }
            else
            {
                var fakerType = parameterType.GetGenericTypeDefinition().MakeGenericType(modelType);
                faker = CreateFaker(fakerType);
            }

            var fakerImpl =
                Activator.CreateInstance(fakerImplSubclassType, fakerImplementations, faker) as FakerImplementationBase;
            fakerImplementations[modelType] = fakerImpl ?? throw new Exception("Faker implementation cannot be null");
        }

        return fakerImplementations;
    }

    private object ResolveFaker(Type parameterType)
    {
        var implementations = CreateFakerImplementations(parameterType);
        return implementations[parameterType.GenericTypeArguments[0]].CreateFakerObj();
    }

    private FakerImplementationBase ResolveFakerImpl(Type parameterType)
    {
        var baseType = parameterType.BaseType;
        if (baseType is null || baseType.GetGenericTypeDefinition() != typeof(FakerImplementationBase<>))
            throw new Exception("Faker implementation must inherit from FakerImplementationBase<>");

        var implementations = CreateFakerImplementations(parameterType, true);
        return implementations[baseType.GenericTypeArguments[0]];
    }

    private void RegisterBaseServices(IServiceCollection services)
    {
        services.AddSingleton(_context);
        services.AddSingleton<IEncryptionKeyProvider>(new MockEncryptionKeyProvider());
        var config = new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>("AppDomain", "localhost"),
            new KeyValuePair<string, string?>("CorsOrigin", "http://localhost:4200"),
            new KeyValuePair<string, string?>("AccessTokenExpiry", "5m"),
            new KeyValuePair<string, string?>("DataDir", _testFolder),
            new KeyValuePair<string, string?>("JWT_PRIVATE_KEY", "test_jwt_private_key_123456789234234247")
        ]).Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(AppSettings.Initialize);
        services.AddLogging();
        
        services.AddScopedSubstitute<IConnectionMultiplexer>();
        services.AddScoped<ISubscriber>(implementationFactory: serviceProvider =>
        {
            var muxer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var subscriber = Substitute.For<ISubscriber>();
            muxer.GetSubscriber().Returns(subscriber);
            return subscriber;
        });

        var classType = _methodInfo.DeclaringType;
        if (classType is null)
            throw new Exception("Declaring type cannot be null in this context");

        var concreteTypeAttributes = classType.GetCustomAttributes<ConcreteTypeAttribute>();
        foreach (var concreteTypeAttribute in concreteTypeAttributes)
        {
            var declaringType = concreteTypeAttribute.DeclaringType
                                ?? throw new Exception(
                                    "ConcreteTypeAttribute DeclaringType cannot be null in this context");
            services.AddScoped(declaringType, concreteTypeAttribute.ImplementationType);
        }

        // Find the ConfigurationDataAttribute
        if (classType.GetCustomAttribute<ConfigurationDataAttribute>() is { } configurationDataAttribute)
        {
            var functionName = configurationDataAttribute.FunctionName;
            var function = _methodInfo.DeclaringType?.GetMethod(functionName);
            if (function is null)
                throw new Exception($"Function {functionName} not found on class {classType.FullName}");

            // Check if the function is static
            if (!function.IsStatic)
                throw new Exception($"Function {functionName} must be static");

            if (!typeof(IConfiguration).IsAssignableFrom(function.ReturnType)
                || function.Invoke(null, null) is not IConfiguration configuration)
                throw new Exception($"Function {functionName} must return IConfiguration");

            services.AddSingleton(configuration);
        }

        // Find the "System under test" type
        var sutParam = _methodInfo.GetParameters()
            .FirstOrDefault(p => p.ParameterType.GetGenericTypeDefinition() == typeof(Sut<>));
        if (sutParam is not null)
        {
            var sutTypeParam = sutParam.ParameterType.GenericTypeArguments.First();
            services.AddScoped(sutTypeParam);
            return;
        }

        if (classType.GetCustomAttribute<SutAttribute>() is not { } sutAttribute)
            throw new Exception(
                $"SutAttribute or Sut<> parameter not found on class {classType.FullName} or test method {_methodInfo.Name}");

        var sutType = sutAttribute.SutType;
        services.AddScoped(sutType);
    }

    private static bool IsAlreadyRegistered(IServiceCollection services, Type serviceType)
    {
        // Check that the interface is not already registered
        return services.Any(service => service.ServiceType == serviceType) ||
               // Check that the interface is not already registered with a concrete type
               services.Any(service => service.ServiceType.IsAssignableFrom(serviceType));
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        RegisterBaseServices(services);

        // Check if any service has been registered with the concrete type attribute
        foreach (var parameter in _methodParameters)
        {
            if (parameter.GetCustomAttribute<ConcreteTypeAttribute>() is { } customTypeAttribute)
            {
                if (IsAlreadyRegistered(services, parameter.ParameterType))
                    throw new Exception($"Service of type {parameter.ParameterType} already registered.");

                services.AddScoped(parameter.ParameterType, customTypeAttribute.ImplementationType);
            }
            // else if (!IsAlreadyRegistered(services, parameter.ParameterType))
            // {
            //     services.AddScoped(parameter.ParameterType);
            // }
        }

        // Add the rest of interfaces
        foreach (var mockInterfaceType in MockInterfaceList.Where(mockInterfaceType =>
                     !IsAlreadyRegistered(services, mockInterfaceType)))
        {
            services.AddScoped(
                mockInterfaceType,
                implementationFactory: _ => Substitute.For([mockInterfaceType], [])
            );
            _realizedMocks.Add(mockInterfaceType);
        }

        _rootProvider = services.BuildServiceProvider(validateScopes: true);
        _serviceScope = _rootProvider.CreateScope();

        return _serviceScope.ServiceProvider;
    }

    public async ValueTask DisposeAsync()
    {
        await _testDatabaseFixture.DisposeAsync();
        await _context.DisposeAsync();
        _serviceScope?.Dispose();
    }

    private static readonly Random Rnd = new();

    private static string RandomString(int length)
    {
        lock (Rnd)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[Rnd.Next(s.Length)]).ToArray())
                .ToLowerInvariant();
        }
    }

    private async Task<User> CreateUser(string username, string[] roles, string? parameterName)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        var user = await AddUserToDb(username, roles);
        _createdUsers.Add(user);
        if (parameterName is not null)
            _paramNameToUser[parameterName] = user;

        if (_createdUsers.Count > 1 || !_realizedMocks.Contains(typeof(IUserService))) return user;

        // The first user will be set in the UserService by default
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        userService.GetCurrentUser().Returns(user);

        return user;
    }

    private async Task<User> AddUserToDb(string username, params string[] roles)
    {
        // Add test user to db
        var newUser = new User(username, username, $"{username}@test.com", "password")
        {
            Roles = roles
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return newUser;
    }

    private async Task<UserGroup> CreateRandomGroup(string? groupName, User owner)
    {
        ArgumentNullException.ThrowIfNull(_serviceProvider);

        if (groupName is null)
        {
            var rnd = RandomString(32);
            groupName = $"test_group_{rnd}";
        }

        var encryptionService = _serviceProvider.GetRequiredService<IEncryptionService>();

        var userGroupService = new UserGroupService(
            _context,
            encryptionService,
            _serviceProvider.GetRequiredService<IUserService>()
        );
        await userGroupService.Create(owner, groupName);

        var dbGroup = await _context.UserGroups
            .Where(group => group.Name == encryptionService.EncryptAesStringBase64(groupName))
            .FirstOrDefaultAsync();

        Assert.NotNull(dbGroup);
        return dbGroup;
    }
}
