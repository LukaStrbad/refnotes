using Server;
using Server.Services;
using ServerTests.Extensions;

namespace ServerTests.ServiceTests;

using System.Text;
using Xunit;

public class EncryptionServiceTests : BaseTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _encryptionService = new EncryptionService(AesKey, AesIv);
    }

    [Fact]
    public void EncryptionServiceConstructor_WithAppConfig_InitializesKeyAndIv()
    {
        const string baseDir = ".";
        var appConfig = new AppConfiguration { BaseDir = baseDir };

        // Create files in the base directory to simulate existing key and IV files.
        var keyPath = Path.Combine(baseDir, EncryptionService.AesKeyFileName);
        var ivPath = Path.Combine(baseDir, EncryptionService.AesIvFileName);
        File.WriteAllBytes(keyPath, AesKey);
        File.WriteAllBytes(ivPath, AesIv);

        var encryptionService = new EncryptionService(appConfig);

        Assert.NotNull(encryptionService);
        Assert.Equal(AesKey, encryptionService.AesKey);
        Assert.Equal(AesIv, encryptionService.AesIv);

        // Clean up the files created for this test.
        File.Delete(keyPath);
        File.Delete(ivPath);
    }

    [Fact]
    public void EncryptionServiceConstructor_WithAppConfigNoFiles_InitializesKeyAndIv()
    {
        const string baseDir = ".";
        var appConfig = new AppConfiguration { BaseDir = baseDir };

        var encryptionService = new EncryptionService(appConfig);

        Assert.NotNull(encryptionService);
        Assert.NotEqual(AesKey, encryptionService.AesKey);
        Assert.NotEqual(AesIv, encryptionService.AesIv);
    }

    [Fact]
    public void EncryptAes_EncryptsByteArray_ReturnsEncryptedByteArray()
    {
        var bytes = "test data"u8.ToArray();

        var encryptedBytes = _encryptionService.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
    }

    [Fact]
    public async Task EncryptAes_EncryptsStream_ReturnsEncryptedStream()
    {
        var bytes = "test data"u8.ToArray();
        await using var inputStream = new MemoryStream(bytes);
        await using var outputStream = new MemoryStream();

        await _encryptionService.EncryptAesToStreamAsync(inputStream, outputStream);

        var encryptedBytes = outputStream.ToArray();
        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
    }

    [Fact]
    public void EncryptAes_EncryptsString_ReturnsEncryptedByteArray()
    {
        const string text = "test data";

        var encryptedBytes = _encryptionService.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(Encoding.UTF8.GetBytes(text), encryptedBytes);
    }

    [Fact]
    public void EncryptAesStringBase64_EncryptsString_ReturnsBase64String()
    {
        const string text = "test data";

        var encryptedBase64 = _encryptionService.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEqual(text, encryptedBase64);
    }

    [Fact]
    public void DecryptAes_DecryptsEncryptedByteArray_ReturnsOriginalByteArray()
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = _encryptionService.EncryptAes(bytes);

        var decryptedBytes = _encryptionService.DecryptAes(encryptedBytes);

        Assert.Equal(bytes, decryptedBytes);
    }

    [Fact]
    public void DecryptAes_DecryptsEncryptedStream_ReturnsOriginalStream()
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = _encryptionService.EncryptAes(bytes);
        
        using var encryptedInputStream = new MemoryStream(encryptedBytes);

        using var outputStream = _encryptionService.DecryptAesToStream(encryptedInputStream);

        var decryptedBytes = outputStream.ToArray();
        Assert.Equal(bytes, decryptedBytes);
    }

    [Fact]
    public void DecryptAesString_DecryptsEncryptedByteArray_ReturnsOriginalString()
    {
        const string text = "test data";
        var encryptedBytes = _encryptionService.EncryptAes(text);

        var decryptedText = _encryptionService.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void DecryptAesStringBase64_DecryptsEncryptedBase64String_ReturnsOriginalString()
    {
        const string text = "test data";
        var encryptedBase64 = _encryptionService.EncryptAesStringBase64(text);

        var decryptedText = _encryptionService.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void EncryptAes_EmptyByteArray_ReturnsNonEmptyEncryptedByteArray()
    {
        var bytes = Array.Empty<byte>();

        var encryptedBytes = _encryptionService.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Fact]
    public void EncryptAes_EmptyString_ReturnsNonEmptyEncryptedByteArray()
    {
        var text = string.Empty;

        var encryptedBytes = _encryptionService.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Fact]
    public void EncryptAesStringBase64_EmptyString_ReturnsNonEmptyBase64String()
    {
        var text = string.Empty;

        var encryptedBase64 = _encryptionService.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEmpty(encryptedBase64);
    }

    [Fact]
    public void DecryptAes_EmptyEncryptedByteArray_ReturnsEmptyByteArray()
    {
        var bytes = Array.Empty<byte>();
        var encryptedBytes = _encryptionService.EncryptAes(bytes);

        var decryptedBytes = _encryptionService.DecryptAes(encryptedBytes);

        Assert.Empty(decryptedBytes);
    }

    [Fact]
    public void DecryptAesString_EmptyEncryptedByteArray_ReturnsEmptyString()
    {
        var text = string.Empty;
        var encryptedBytes = _encryptionService.EncryptAes(text);

        var decryptedText = _encryptionService.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void DecryptAesStringBase64_EmptyEncryptedBase64String_ReturnsEmptyString()
    {
        var text = string.Empty;
        var encryptedBase64 = _encryptionService.EncryptAesStringBase64(text);

        var decryptedText = _encryptionService.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }
}
