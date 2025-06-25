using System.Security.Cryptography;

namespace Api.Services;

public sealed class EncryptionKeyProvider : IEncryptionKeyProvider
{
    public byte[] Key { get; }
    public byte[] Iv { get; }
    
    public const string AesKeyFileName = "aes_key.bin";
    public const string AesIvFileName = "aes_iv.bin";

    public EncryptionKeyProvider(AppConfiguration appConfig)
    {
        var keyPath = Path.Combine(appConfig.BaseDir, AesKeyFileName);
        var ivPath = Path.Combine(appConfig.BaseDir, AesIvFileName);
        if (File.Exists(keyPath) && File.Exists(ivPath))
        {
            Key = File.ReadAllBytes(keyPath);
            Iv = File.ReadAllBytes(ivPath);
        }
        else
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();
            Key = aes.Key;
            Iv = aes.IV;
            File.WriteAllBytes(keyPath, Key);
            File.WriteAllBytes(ivPath, Iv);
        }
    }
}