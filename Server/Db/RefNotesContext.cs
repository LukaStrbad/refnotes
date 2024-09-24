using Microsoft.EntityFrameworkCore;
using Server.Model;

namespace Server.Db;

public class RefNotesContext(AppConfiguration appConfig) : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<EncryptedDirectory> Directories { get; set; }
    
    public string DbPath { get; } = Path.Join(appConfig.BaseDir, "refnotes.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}