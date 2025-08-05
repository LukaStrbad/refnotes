using Api.Services;
using Microsoft.Extensions.Configuration;

namespace Api.Tests.ServiceTests;

public sealed class EncryptionKeyProviderTests
{
    private readonly byte[] _aesKey = "1234567890123456"u8.ToArray();
    private readonly byte[] _sha256Key = "1234567890123456"u8.ToArray();
    private readonly byte[] _aesIv = "1234567890123456"u8.ToArray();

    [Fact]
    public void EncryptionKeyProvider_WithConfig_InitializesKeyAndIv()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection([
            new KeyValuePair<string, string?>("AES_KEY", Convert.ToBase64String(_aesKey)),
            new KeyValuePair<string, string?>("SHA256_KEY", Convert.ToBase64String(_sha256Key)),
            new KeyValuePair<string, string?>("AES_IV", Convert.ToBase64String(_aesIv))
        ]).Build();

        var encryptionKeyProvider = new EncryptionKeyProvider(config);

        Assert.NotNull(encryptionKeyProvider);
        Assert.Equal(_aesKey, encryptionKeyProvider.AesKey);
        Assert.Equal(_sha256Key, encryptionKeyProvider.Sha256Key);
        Assert.Equal(_aesIv, encryptionKeyProvider.Iv);
    }

    [Fact]
    public void EncryptionKeyProvider_WithoutConfig_ThrowsException()
    {
        var config = new ConfigurationBuilder().Build();
        Assert.Throws<Exception>(() => new EncryptionKeyProvider(config));
    }
}
