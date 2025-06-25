using Api.Services;
using Api;

namespace Api.Tests.ServiceTests;

public sealed class EncryptionKeyProviderTests
{
    private readonly byte[] _aesKey = "1234567890123456"u8.ToArray();
    private readonly byte[] _aesIv = "1234567890123456"u8.ToArray();

    [Fact]
    public void EncryptionKeyProvider_WithAppConfig_InitializesKeyAndIv()
    {
        const string baseDir = ".";
        var appConfig = new AppConfiguration { BaseDir = baseDir };

        // Create files in the base directory to simulate existing key and IV files.
        var keyPath = Path.Combine(baseDir, EncryptionKeyProvider.AesKeyFileName);
        var ivPath = Path.Combine(baseDir, EncryptionKeyProvider.AesIvFileName);
        File.WriteAllBytes(keyPath, _aesKey);
        File.WriteAllBytes(ivPath, _aesIv);

        var encryptionKeyProvider = new EncryptionKeyProvider(appConfig);

        Assert.NotNull(encryptionKeyProvider);
        Assert.Equal(_aesKey, encryptionKeyProvider.Key);
        Assert.Equal(_aesIv, encryptionKeyProvider.Iv);

        // Clean up the files created for this test.
        File.Delete(keyPath);
        File.Delete(ivPath);
    }

    [Fact]
    public void EncryptionKeyProvider_WithAppConfigNoFiles_InitializesKeyAndIv()
    {
        const string baseDir = ".";
        var appConfig = new AppConfiguration { BaseDir = baseDir };

        var encryptionKeyProvider = new EncryptionKeyProvider(appConfig);

        Assert.NotNull(encryptionKeyProvider);
        Assert.NotEqual(_aesKey, encryptionKeyProvider.Key);
        Assert.NotEqual(_aesIv, encryptionKeyProvider.Iv);
    }
}