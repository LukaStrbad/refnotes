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
    private DbContextOptions<RefNotesContext>? _dbOptions;

    private readonly Lock _dbOptionsLock = new();

    private bool _reuseTestContainer;

    public RefNotesContext CreateContext()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new Exception("Connection string is not set");
        }

        lock (_dbOptionsLock)
        {
            if (_dbOptions is not null)
                return new RefNotesContext(_dbOptions);

            var serverVersion = ServerVersion.AutoDetect(_connectionString);
            _dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
                .UseMySql(_connectionString, serverVersion).Options;
            var context = new RefNotesContext(_dbOptions);

            context.Database.EnsureDeleted();
            context.Database.Migrate();

            return context;
        }
    }

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _reuseTestContainer = config.GetValue<bool>("Testcontainers:Reuse");
        _mysqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.4")
            .WithTmpfsMount("/var/lib/mysql")
            .WithReuse(_reuseTestContainer)
            .Build();

        await _mysqlContainer.StartAsync();
        _connectionString = _mysqlContainer.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_mysqlContainer is not null && !_reuseTestContainer)
        {
            await _mysqlContainer.StopAsync();
        }
    }
}
