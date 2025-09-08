using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    public DbSet<PublicFileImage> PublicFileImages { get; set; }
    public DbSet<FileFavorite> FileFavorites { get; set; }
    public DbSet<DirectoryFavorite> DirectoryFavorites { get; set; }
    public DbSet<EmailConfirmToken> EmailConfirmTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<SharedFile> SharedFiles { get; set; }
    public DbSet<SharedFileHash> SharedFileHashes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncryptedFile>()
            .HasMany(left => left.Tags)
            .WithMany(right => right.Files)
            .UsingEntity(join => join.ToTable("encrypted_files_file_tags"));

        new UserGroupRoleConfiguration().Configure(modelBuilder.Entity<UserGroupRole>());

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsKeyless)
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
