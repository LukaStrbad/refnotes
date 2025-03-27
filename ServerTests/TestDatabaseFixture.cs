using Microsoft.EntityFrameworkCore;
using Server.Db;
using ServerTests;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(TestDatabaseFixture))]

namespace ServerTests;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mysqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .Build();
    
    private string? _connectionString;

    public RefNotesContext CreateContext()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new Exception("Connection string is not set");
        }
        var serverVersion = ServerVersion.AutoDetect(_connectionString);
        var dbOptions = new DbContextOptionsBuilder<RefNotesContext>()
            .UseMySql(_connectionString, serverVersion).Options;
        return new RefNotesContext(dbOptions);
    }

    public async ValueTask InitializeAsync()
    {
        await _mysqlContainer.StartAsync();

        _connectionString = _mysqlContainer.GetConnectionString();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _mysqlContainer.StopAsync();
        GC.SuppressFinalize(this);
    }
}