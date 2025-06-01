using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Server.Db;
using Server.Db.Model;
using Server.Model;
using Server.Services;
using Server.Utils;
using ServerTests.Data.Attributes;
using ServerTests.Fixtures;
using ServerTests.Mocks;

namespace ServerTests.Data;

public sealed class AutoDataResolver : IAsyncDisposable
{
    private readonly MethodInfo _methodInfo;
    private readonly ParameterInfo[] _methodParameters;
    private readonly TestDatabaseFixture _testDatabaseFixture;
    private readonly RefNotesContext _context;

    private IServiceProvider? _rootProvider;
    private IServiceScope? _serviceScope;
    private IServiceProvider? _serviceProvider;

    private static readonly List<Type> MockInterfaceList =
    [
        typeof(IUserService),
        typeof(IEncryptionService),
        typeof(IFileStorageService),
        typeof(IFileServiceUtils),
        typeof(IUserGroupService),
        typeof(IBrowserService)
    ];

    private readonly List<Type> _realizedMocks = [];
    private readonly List<User> _createdUsers = [];
    private readonly Dictionary<string, User> _paramNameToUser = [];

    public AutoDataResolver(MethodInfo methodInfo, TestDatabaseFixture testDatabaseFixture)
    {
        _methodInfo = methodInfo;
        _methodParameters = methodInfo.GetParameters();
        _testDatabaseFixture = testDatabaseFixture;

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

        if (parameter.GetCustomAttribute<RandomStringAttribute>() is { } randomStringAttribute)
        {
            return randomStringAttribute.Prefix + RandomString(randomStringAttribute.Length);
        }

        var sutType = typeof(Sut<>);
        if (parameter.ParameterType.GetGenericTypeDefinition() == sutType)
        {
            var genericType = parameter.ParameterType.GenericTypeArguments.First();
            var sutValue = _serviceProvider.GetRequiredService(genericType);
            return Activator.CreateInstance(parameter.ParameterType, sutValue, _context, _serviceProvider,
                _createdUsers);
        }

        return _serviceProvider.GetRequiredService(parameter.ParameterType);
    }

    private void RegisterBaseServices(IServiceCollection services)
    {
        services.AddSingleton(_context);
        services.AddSingleton<IEncryptionKeyProvider>(new MockEncryptionKeyProvider());

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
            else if (!IsAlreadyRegistered(services, parameter.ParameterType))
            {
                services.AddScoped(parameter.ParameterType);
            }
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
        userService.GetUser().Returns(user);

        return user;
    }

    private async Task<User> AddUserToDb(string username, params string[] roles)
    {
        // Add test user to db
        var newUser = new User(0, username, username, $"{username}@test.com", "password")
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