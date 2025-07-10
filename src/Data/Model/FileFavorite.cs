using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("file_favorites")]
public class FileFavorite
{
    public int Id { get; init; }

    [ForeignKey(nameof(User))] public int UserId { get; init; }
    [ForeignKey(nameof(EncryptedFile))] public int EncryptedFileId { get; init; }

    public User? User { get; init; }
    public EncryptedFile? EncryptedFile { get; init; }

    public DateTime FavoriteDate { get; init; } = DateTime.UtcNow;
}
