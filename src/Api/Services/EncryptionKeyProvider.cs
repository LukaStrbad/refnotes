using System.Security.Cryptography;

namespace Api.Services;

public sealed class EncryptionKeyProvider : IEncryptionKeyProvider
{
    public byte[] AesKey { get; }
    public byte[] Sha256Key { get; }
    public byte[] Iv { get; }

    public EncryptionKeyProvider(IConfiguration configuration)
    {
        using var aes = Aes.Create();

        var key = configuration.GetValue<string>("AES_KEY") ?? throw new Exception("AES_KEY configuration is missing.");
        var keyBytes = Convert.FromBase64String(key);
        if (!aes.ValidKeySize(keyBytes.Length * 8))
        {
            var validKeySizes = new List<int>();
            foreach (var keySizes in aes.LegalKeySizes)
            {
                for (var size = keySizes.MinSize; size <= keySizes.MaxSize; size += keySizes.SkipSize)
                    validKeySizes.Add(size);
            }

            var validKeySizesStr = string.Join(", ", validKeySizes);
            throw new Exception($"Invalid AES key size. Valid sizes are: {validKeySizesStr} bits.");
        }
        
        var iv = configuration.GetValue<string>("AES_IV") ?? throw new Exception("AES_IV configuration is missing.");
        var ivBytes = Convert.FromBase64String(iv);
        if (ivBytes.Length != aes.BlockSize / 8)
        {
            throw new Exception($"Invalid AES IV size. Expected {aes.BlockSize / 8} bytes, but got {ivBytes.Length} bytes.");
        }
        
        var sha256Key = configuration.GetValue<string>("SHA256_KEY") ?? throw new Exception("SHA256_KEY configuration is missing.");
        var sha256KeyBytes = Convert.FromBase64String(sha256Key);

        AesKey = keyBytes;
        Iv = ivBytes;
        Sha256Key = sha256KeyBytes;
    }
}
