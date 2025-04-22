using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

public class EncryptionService : IEncryptionService
{
    public byte[] AesKey { get; }
    public byte[] AesIv { get; }

    public const string AesKeyFileName = "aes_key.bin";
    public const string AesIvFileName = "aes_iv.bin";

    public EncryptionService(AppConfiguration appConfig)
    {
        var keyPath = Path.Combine(appConfig.BaseDir, AesKeyFileName);
        var ivPath = Path.Combine(appConfig.BaseDir, AesIvFileName);
        if (File.Exists(keyPath) && File.Exists(ivPath))
        {
            AesKey = File.ReadAllBytes(keyPath);
            AesIv = File.ReadAllBytes(ivPath);
        }
        else
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            AesKey = aes.Key;
            AesIv = aes.IV;
            File.WriteAllBytes(keyPath, AesKey);
            File.WriteAllBytes(ivPath, AesIv);
        }
    }

    public EncryptionService(byte[] aesKey, byte[] aesIv)
    {
        AesKey = aesKey;
        AesIv = aesIv;
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
        using var csEncrypt = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
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
        var decryptor = aesAlg.CreateDecryptor();

        // Create the streams used for decryption.
        using var msDecrypt = new MemoryStream(encryptedBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        // Read the decrypted bytes from the decrypting stream
        // and place them in a string.
        var buffer = new byte[1024];
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
