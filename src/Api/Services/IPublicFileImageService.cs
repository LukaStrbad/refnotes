namespace Api.Services;

public interface IPublicFileImageService
{
    Task UpdateImagesForPublicFile(int publicFileId);
    Task RemoveImagesForEncryptedFile(int encryptedFileId);
    Task RemoveImagesForPublicFile(int publicFileId);
}
