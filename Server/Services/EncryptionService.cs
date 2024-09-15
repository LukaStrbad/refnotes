using System.Security.Cryptography;
using System.Text;

namespace Server.Services;

public class EncryptionService
{
    private readonly byte[] _aesKey;
    private readonly byte[] _aesIv;

    public EncryptionService()
    {
        var keyPath = Path.Combine(Configuration.RefnotesPath, "aes_key.bin");
        var ivPath = Path.Combine(Configuration.RefnotesPath, "aes_iv.bin");
        if (File.Exists(keyPath) && File.Exists(ivPath))
        {
            _aesKey = File.ReadAllBytes(keyPath);
            _aesIv = File.ReadAllBytes(ivPath);
        }
        else
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            _aesKey = aes.Key;
            _aesIv = aes.IV;
            File.WriteAllBytes(keyPath, _aesKey);
            File.WriteAllBytes(ivPath, _aesIv);
        }
    }

    public byte[] EncryptAes(byte[] bytes)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _aesKey;
        aesAlg.IV = _aesIv;

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

    public byte[] EncryptAes(string text) => EncryptAes(Encoding.UTF8.GetBytes(text));
    
    public string EncryptAesStringBase64(string text) =>
        Convert.ToBase64String(EncryptAes(text));

    public IEnumerable<byte> DecryptAes(byte[] encryptedBytes)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _aesKey;
        aesAlg.IV = _aesIv;

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

    public string DecryptAesString(byte[] encryptedBytes) =>
        Encoding.UTF8.GetString(DecryptAes(encryptedBytes).ToArray());
}