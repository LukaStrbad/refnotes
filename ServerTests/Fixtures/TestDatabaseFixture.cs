using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Server.Db;
using ServerTests.Fixtures;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(TestDatabaseFixture))]

namespace ServerTests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseFixture : IAsyncLifetime
{
    private MySqlContainer? _mysqlContainer;

    private string? _connectionString;

    private bool _isDatabaseCreated;
    private readonly Lock _isDatabaseCreatedLock = new();

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
            context.Database.EnsureCreated();
            _isDatabaseCreated = true;

            return context;
        }
    }

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        var connectionString = config["Db:ConnectionString"];

        // Start a test container if no connection string is provided
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _mysqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.4")
                .WithReuse(true)
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
        if (_mysqlContainer is not null)
        {
            await _mysqlContainer.StopAsync();
        }
    }
}