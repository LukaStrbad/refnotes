using Microsoft.EntityFrameworkCore;
using Server.Db;
using Testcontainers.MySql;

namespace ServerTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseFixture : IAsyncLifetime
{
    public RefNotesContext Context { get; private set; } = null!;

    private readonly MySqlContainer _mysqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .Build();

    public async Task InitializeAsync()
    {
        await _mysqlContainer.StartAsync();

        var connectionString = _mysqlContainer.GetConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("Connection string is not set");
        }

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        // Create test db
        var dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
            .UseMySql(connectionString, serverVersion).Options;
        Context = new RefNotesContext(dbOptions);
        await Context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        return _mysqlContainer?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }
}