namespace Server.Services;

/// <summary>
/// Interface for encryption services.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the given byte array using AES encryption.
    /// </summary>
    /// <param name="bytes">The byte array to encrypt.</param>
    /// <returns>The encrypted byte array.</returns>
    byte[] EncryptAes(byte[] bytes);

    /// <summary>
    /// Encrypts the given text using AES encryption.
    /// Text is encoded as UTF-8.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted byte array.</returns>
    byte[] EncryptAes(string text);

    /// <summary>
    /// Encrypts the given input stream using AES encryption and writes the result to the output stream.
    /// </summary>
    /// <param name="inputStream">The input stream to encrypt.</param>
    /// <param name="outputStream">The encrypted output stream</param>
    public Task EncryptAesToStreamAsync(Stream inputStream, Stream outputStream);

    /// <summary>
    /// Encrypts the given text using AES encryption and returns the result as a Base64 string.
    /// Text is encoded as UTF-8.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted text as a Base64 string.</returns>
    string EncryptAesStringBase64(string text);

    /// <summary>
    /// Decrypts the given encrypted byte array using AES decryption.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted byte array to decrypt.</param>
    /// <returns>The decrypted byte array.</returns>
    IEnumerable<byte> DecryptAes(byte[] encryptedBytes);

    /// <summary>
    /// Decrypts the given stream using AES decryption and writes the result to the output stream.
    /// </summary>
    /// <param name="encryptedInputStream">The encrypted input stream</param>
    /// <returns>The decrypted output stream</returns>
    public Stream DecryptAesToStream(Stream encryptedInputStream);

    /// <summary>
    /// Decrypts the given encrypted byte array using AES decryption and returns the result as a string.
    /// Text is decoded as UTF-8.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted byte array to decrypt.</param>
    /// <returns>The decrypted text as a string.</returns>
    string DecryptAesString(byte[] encryptedBytes);

    /// <summary>
    /// Decrypts the given encrypted text (Base64 encoded) using AES decryption and returns the result as a string.
    /// Text is decoded as UTF-8.
    /// </summary>
    /// <param name="encryptedText">The encrypted text (Base64 encoded) to decrypt.</param>
    /// <returns>The decrypted text as a string.</returns>
    string DecryptAesStringBase64(string encryptedText);
}
