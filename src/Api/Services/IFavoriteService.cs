using Api.Model;
using Data.Model;

namespace Api.Services;

/// <summary>
/// Provides functionalities for managing favorite files and directories.
/// This service allows users to mark files and directories as favorites for quick access.
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Marks a file as a favorite for the current user.
    /// </summary>
    /// <param name="file">The encrypted file to be marked as favorite.</param>
    Task FavoriteFile(EncryptedFile file);

    /// <summary>
    /// Removes a file from the current user's favorite list.
    /// </summary>
    /// <param name="file">The encrypted file to be removed from favorites.</param>
    Task UnfavoriteFile(EncryptedFile file);

    /// <summary>
    /// Retrieves all files marked as favorites by the current user.
    /// </summary>
    Task<List<FileFavoriteDetails>> GetFavoriteFiles();

    /// <summary>
    /// Marks a directory as a favorite for the current user.
    /// </summary>
    /// <param name="directory">The encrypted directory to be marked as favorite.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FavoriteDirectory(EncryptedDirectory directory);

    /// <summary>
    /// Removes a directory from the current user's favorites list.
    /// </summary>
    /// <param name="directory">The encrypted directory to be removed from favorites.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnfavoriteDirectory(EncryptedDirectory directory);

    /// <summary>
    /// Retrieves all directories marked as favorites by the current user.
    /// </summary>
    Task<List<DirectoryFavoriteDetails>> GetFavoriteDirectories();
}
