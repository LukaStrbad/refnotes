using System.Security.Cryptography;
using System.Text;

namespace Api.Services;

public class EncryptionService : IEncryptionService
{
    private byte[] AesKey { get; }
    private byte[] AesIv { get; }

    public EncryptionService(byte[] aesKey, byte[] aesIv)
    {
        AesKey = aesKey;
        AesIv = aesIv;
    }

    public EncryptionService(IEncryptionKeyProvider keyProvider)
    {
        AesKey = keyProvider.Key;
        AesIv = keyProvider.Iv;
    }

    public byte[] EncryptAes(byte[] bytes)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = AesKey;
        aesAlg.IV = AesIv;

        // Create an encryptor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor();

        // Create the streams used for encryption.
        using var msEncrypt = new MemoryStream(bytes.Length + aesAlg.BlockSize / 8);
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
        aesAlg.IV = AesIv;

        // Create an encryptor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor();

        // Create the streams used for encryption.
        await using var csEncrypt = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
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
        aesAlg.IV = AesIv;

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
}
