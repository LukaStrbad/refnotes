using Microsoft.EntityFrameworkCore;
using Server.Model;

namespace Server.Db;

public class RefNotesContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public string DbPath { get; } = Path.Join(Configuration.RefnotesPath, "refnotes.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}