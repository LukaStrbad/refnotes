using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AppHost;

/// <summary>
/// Factory for creating RefNotesContext for design-time tools like EF Core CLI.
/// Used only for setting up the connection string.
/// This is necessary because, typically, the path is specified by AppConfiguration, which is not available at design time.
/// </summary>
// ReSharper disable once UnusedType.Global
public class RefNotesContextFactory : IDesignTimeDbContextFactory<RefNotesContext>
{
    public RefNotesContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        
        // Get connection string from above sources
        var connectionString = configuration.GetConnectionString("main");
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Connection string is not set");
        
        var optionsBuilder = new DbContextOptionsBuilder<RefNotesContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new RefNotesContext(optionsBuilder.Options);
    }
}