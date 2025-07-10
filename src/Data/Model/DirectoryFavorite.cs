using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("directory_favorites")]
public class DirectoryFavorite
{
    public int Id { get; init; }

    [ForeignKey(nameof(User))] public int UserId { get; init; }

    [ForeignKey(nameof(EncryptedDirectory))]
    public int EncryptedDirectoryId { get; init; }

    public User? User { get; init; }
    public EncryptedDirectory? EncryptedDirectory { get; init; }

    public DateTime FavoriteDate { get; init; } = DateTime.UtcNow;
}
