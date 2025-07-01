using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Model;

[Table("public_file_images")]
public class PublicFileImage(int publicFileId, int encryptedFileId)
{
    public int Id { get; init; }

    [ForeignKey("PublicFileId")] public PublicFile? PublicFile { get; init; }
    [ForeignKey("PublicFileId")] public int PublicFileId { get; init; } = publicFileId;

    [ForeignKey("EncryptedFileId")] public EncryptedFile? EncryptedFile { get; init; }
    [ForeignKey("EncryptedFileId")] public int EncryptedFileId { get; init; } = encryptedFileId;
}
