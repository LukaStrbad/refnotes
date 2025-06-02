using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Server.Model;

namespace Server.Services;

public class EncryptionService : IEncryptionService
{
    public byte[] AesKey { get; }
    public byte[] AesIv { get; }

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
        using var msEncrypt = new MemoryStream();
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

    public IEnumerable<byte> DecryptAes(byte[] encryptedBytes)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = AesKey;
        aesAlg.IV = AesIv;

        // Create a decryptor to perform the stream transform.
        using var decryptor = aesAlg.CreateDecryptor();

        // Create the streams used for decryption.
        using var msDecrypt = new MemoryStream(encryptedBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        // Read the decrypted bytes from the decrypting stream
        // and place them in a string.
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            while (csDecrypt.CanRead)
            {
                var bytesRead = csDecrypt.Read(buffer);

                if (bytesRead == 0)
                {
                    yield break;
                }

                for (var i = 0; i < bytesRead; i++)
                {
                    yield return buffer[i];
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
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
        Encoding.UTF8.GetString(DecryptAes(encryptedBytes).ToArray());

    public string DecryptAesStringBase64(string encryptedText)
    {
        var bytes = Convert.FromBase64String(encryptedText);
        return DecryptAesString(bytes);
    }
}
