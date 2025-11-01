using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Api.Tests.Fixtures;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(TestDatabaseFixture))]

namespace Api.Tests.Fixtures;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

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

            _dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
                .UseNpgsql(_connectionString, o => o.SetPostgresVersion(18, 0)).Options;
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
        
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithReuse(_reuseTestContainer)
            .WithDatabase("test")
            .Build();

        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_postgresContainer is not null && !_reuseTestContainer)
        {
            await _postgresContainer.StopAsync();
        }
    }
}
