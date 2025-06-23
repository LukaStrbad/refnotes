using Data.Db.Model;

namespace Server.Services;

public interface IPublicFileService
{
    /// <summary>
    /// Gets URL hash from file id.
    /// </summary>
    /// <param name="encryptedFileId">ID of the encrypted file</param>
    /// <returns>The URL hash or null if the public file is not found</returns>
    Task<string?> GetUrlHashAsync(int encryptedFileId);

    /// <summary>
    /// Generate a new public File with a random hash.
    /// </summary>
    /// <remarks>
    /// This method will return an existing hash if one exists.
    /// </remarks>
    /// <param name="encryptedFileId">ID of the encrypted file</param>
    /// <returns>The generated URL hash</returns>
    /// <exception cref="FileNotFoundException">Thrown when an encrypted file with specified ID doesn't exist</exception>
    Task<string> CreatePublicFileAsync(int encryptedFileId);

    /// <summary>
    /// Deletes a public file.
    /// </summary>
    /// <param name="fileId">Public file ID</param>
    /// <returns>True if the public file was deleted, false if it doesn't exist</returns>
    Task<bool> DeactivatePublicFileAsync(int fileId);
    
    Task<EncryptedFile?> GetEncryptedFileAsync(string urlHash);

    Task<bool> IsPublicFileActive(string urlHash);
}