using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("public_file_images")]
public class PublicFileImage
{
    public int Id { get; init; }

    [ForeignKey("PublicFileId")] public PublicFile? PublicFile { get; init; }
    [ForeignKey("PublicFileId")] public required int PublicFileId { get; init; }

    [ForeignKey("EncryptedFileId")] public EncryptedFile? EncryptedFile { get; init; }
    [ForeignKey("EncryptedFileId")] public required int EncryptedFileId { get; init; }
}
