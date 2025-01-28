using Microsoft.EntityFrameworkCore;
using Server.Db.Model;
using Server.Model;

namespace Server.Db;

public class RefNotesContext(DbContextOptions<RefNotesContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<EncryptedDirectory> Directories { get; set; }
    public DbSet<FileTag> FileTags { get; set; }
}