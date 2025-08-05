using System.Security.Cryptography;
using System.Text;

namespace Api.Services;

public class EncryptionService : IEncryptionService
{
    private byte[] AesKey { get; }
    private byte[] Sha256Key { get; }

    public EncryptionService(IEncryptionKeyProvider keyProvider)
    {
        AesKey = keyProvider.AesKey;
        Sha256Key = keyProvider.Sha256Key;
    }

    public byte[] EncryptAes(byte[] bytes)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = AesKey;
        var iv = aesAlg.IV;

        // Create an encryptor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor();

        // Create the streams used for encryption.
        using var msEncrypt = new MemoryStream(bytes.Length + aesAlg.BlockSize / 8);
        // Prepend the IV to the encrypted data.
        msEncrypt.Write(iv);
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            // Write all data to the stream.
            csEncrypt.Write(bytes);
        }

        var encrypted = msEncrypt.ToArray();

        // Return the encrypted bytes from the memory stream.
        return encrypted;
    }

    public async Task EncryptAesToStreamAsync(Stream inputStream, Stream outputStream)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = AesKey;
        var iv = aesAlg.IV;

        // Create an encryptor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor();

        // Create the streams used for encryption.
        await using var csEncrypt = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        // Prepend the IV to the output stream.
        outputStream.Write(iv);
        await inputStream.CopyToAsync(csEncrypt);
    }

    public byte[] EncryptAes(string text) => EncryptAes(Encoding.UTF8.GetBytes(text));

    public string EncryptAesStringBase64(string text) =>
        Convert.ToBase64String(EncryptAes(text));

    public byte[] DecryptAes(byte[] encryptedBytes)
    {
        using var outputStream = new MemoryStream(encryptedBytes.Length);
        var decryptedStream = DecryptAesToStream(new MemoryStream(encryptedBytes));
        decryptedStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    public Stream DecryptAesToStream(Stream encryptedInputStream)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = AesKey;
        // Read the IV from the beginning of the stream.
        byte[] iv = new byte[aesAlg.BlockSize / 8];
        if (encryptedInputStream.Read(iv, 0, iv.Length) != iv.Length)
        {
            throw new InvalidOperationException("Invalid encrypted data: IV not found.");
        }
        aesAlg.IV = iv;

        // Create a decryptor to perform the stream transform.
        var decryptor = aesAlg.CreateDecryptor();

        // Create the streams used for decryption.
        return new CryptoStream(encryptedInputStream, decryptor, CryptoStreamMode.Read);
    }

    public string DecryptAesString(byte[] encryptedBytes) =>
        Encoding.UTF8.GetString(DecryptAes(encryptedBytes));

    public string DecryptAesStringBase64(string encryptedText)
    {
        var bytes = Convert.FromBase64String(encryptedText);
        return DecryptAesString(bytes);
    }

    public string HashString(string text)
    {
        using var sha256 = new HMACSHA256(Sha256Key);
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hashBytes);
    }
}
