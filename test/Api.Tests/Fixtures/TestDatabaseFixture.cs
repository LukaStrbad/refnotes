using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Api.Tests.Fixtures;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(TestDatabaseFixture))]

namespace Api.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseFixture : IAsyncLifetime
{
    private MySqlContainer? _mysqlContainer;

    private string? _connectionString;

    private bool _isDatabaseCreated;
    private readonly Lock _isDatabaseCreatedLock = new();

    private static TestDatabaseFixture? _instance;
    private bool _reuseTestContainer;

    public static TestDatabaseFixture Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new Exception("TestDatabaseFixture is not initialized");
            }

            return _instance;
        }
    }

    public RefNotesContext CreateContext()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new Exception("Connection string is not set");
        }

        lock (_isDatabaseCreatedLock)
        {
            var serverVersion = ServerVersion.AutoDetect(_connectionString);
            var dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
                .UseMySql(_connectionString, serverVersion).Options;
            var context = new RefNotesContext(dbOptions);

            if (_isDatabaseCreated) return context;

            context.Database.EnsureDeleted();
            context.Database.Migrate();
            _isDatabaseCreated = true;

            return context;
        }
    }

    public async ValueTask InitializeAsync()
    {
        _instance = this;

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config["Db:ConnectionString"];
        _reuseTestContainer = config.GetValue<bool>("TestContainers:Reuse");

        // Start a test container if no connection string is provided
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _mysqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.4")
                .WithTmpfsMount("/var/lib/mysql")
                .WithReuse(_reuseTestContainer)
                .Build();

            await _mysqlContainer.StartAsync();
            _connectionString = _mysqlContainer.GetConnectionString();
            return;
        }

        _connectionString = connectionString;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_mysqlContainer is not null && !_reuseTestContainer)
        {
            await _mysqlContainer.StopAsync();
        }

        _instance = null;
    }
}
