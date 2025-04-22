using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Server.Db;

/// <summary>
/// Factory for creating RefNotesContext for design-time tools like EF Core CLI.
/// Used only for setting up the connection string.
/// This is necessary because, typically, the path is specified by AppConfiguration, which is not available at design time.
/// </summary>
[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class RefNotesContextFactory : IDesignTimeDbContextFactory<RefNotesContext>
{
    public RefNotesContext CreateDbContext(string[] args)
    {
        string? connectionString;
        if (args.Length > 0)
        {
            connectionString = args[0];
        }
        else
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json")
                .Build();

            connectionString = configuration["Db:ConnectionString"];
        }

        ArgumentNullException.ThrowIfNull(connectionString);
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        
        var optionsBuilder = new DbContextOptionsBuilder<RefNotesContext>();
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new RefNotesContext(optionsBuilder.Options);
    }
}