using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Server.Db;

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
        var optionsBuilder = new DbContextOptionsBuilder<RefNotesContext>();
        optionsBuilder.UseSqlite("Data Source=refnotes.db");

        return new RefNotesContext(optionsBuilder.Options);
    }
}