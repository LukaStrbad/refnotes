using Data.Model;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Data;

public class RefNotesContext(DbContextOptions<RefNotesContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<EncryptedDirectory> Directories { get; set; }
    public DbSet<EncryptedFile> Files { get; set; }
    public DbSet<FileTag> FileTags { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserGroupRole> UserGroupRoles { get; set; }
    public DbSet<GroupAccessCode> GroupAccessCodes { get; set; }
    public DbSet<PublicFile> PublicFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncryptedFile>()
            .HasMany(left => left.Tags)
            .WithMany(right => right.Files)
            .UsingEntity(join => join.ToTable("encrypted_files_file_tags"));

        new UserGroupRoleConfiguration().Configure(modelBuilder.Entity<UserGroupRole>());
    }
}
