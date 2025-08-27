using System.Security.Cryptography;
using Api.Services;
using Api.Tests.Extensions;
using Api.Tests.Mocks;

namespace Api.Tests.ServiceTests;

using System.Text;
using Xunit;

public class EncryptionServiceTests : BaseTests
{
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        var keyProvider = new MockEncryptionKeyProvider();
        _service = new EncryptionService(keyProvider);
    }

    [Fact]
    public void EncryptAes_EncryptsByteArray_ReturnsEncryptedByteArray()
    {
        var bytes = "test data"u8.ToArray();

        var encryptedBytes = _service.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
    }

    [Fact]
    public async Task EncryptAes_EncryptsStream_ReturnsEncryptedStream()
    {
        var bytes = "test data"u8.ToArray();
        await using var inputStream = new MemoryStream(bytes);
        await using var outputStream = new MemoryStream();

        await _service.EncryptAesToStreamAsync(inputStream, outputStream);

        var encryptedBytes = outputStream.ToArray();
        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
        Assert.Equal(32, encryptedBytes.Length); // IV (16 bytes) + data (padded to 16 bytes)
    }

    [Fact]
    public void EncryptAes_EncryptsString_ReturnsEncryptedByteArray()
    {
        const string text = "test data";

        var encryptedBytes = _service.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(Encoding.UTF8.GetBytes(text), encryptedBytes);
    }

    [Fact]
    public void EncryptAesStringBase64_EncryptsString_ReturnsBase64String()
    {
        const string text = "test data";

        var encryptedBase64 = _service.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEqual(text, encryptedBase64);
    }

    [Fact]
    public void DecryptAes_DecryptsEncryptedByteArray_ReturnsOriginalByteArray()
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = _service.EncryptAes(bytes);

        var decryptedBytes = _service.DecryptAes(encryptedBytes);

        Assert.Equal(bytes, decryptedBytes);
    }

    [Fact]
    public void DecryptAes_DecryptsEncryptedStream_ReturnsOriginalStream()
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = _service.EncryptAes(bytes);

        using var encryptedInputStream = new MemoryStream(encryptedBytes);

        using var outputStream = _service.DecryptAesToStream(encryptedInputStream);

        var decryptedBytes = outputStream.ToArray();
        Assert.Equal(bytes, decryptedBytes);
    }

    [Fact]
    public void DecryptAesString_DecryptsEncryptedByteArray_ReturnsOriginalString()
    {
        const string text = "test data";
        var encryptedBytes = _service.EncryptAes(text);

        var decryptedText = _service.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void DecryptAesStringBase64_DecryptsEncryptedBase64String_ReturnsOriginalString()
    {
        const string text = "test data";
        var encryptedBase64 = _service.EncryptAesStringBase64(text);

        var decryptedText = _service.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void EncryptAes_EmptyByteArray_ReturnsNonEmptyEncryptedByteArray()
    {
        var bytes = Array.Empty<byte>();

        var encryptedBytes = _service.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Fact]
    public void EncryptAes_EmptyString_ReturnsNonEmptyEncryptedByteArray()
    {
        var text = string.Empty;

        var encryptedBytes = _service.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Fact]
    public void EncryptAesStringBase64_EmptyString_ReturnsNonEmptyBase64String()
    {
        var text = string.Empty;

        var encryptedBase64 = _service.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEmpty(encryptedBase64);
    }

    [Fact]
    public void DecryptAes_EmptyEncryptedByteArray_ReturnsEmptyByteArray()
    {
        var bytes = Array.Empty<byte>();
        var encryptedBytes = _service.EncryptAes(bytes);

        var decryptedBytes = _service.DecryptAes(encryptedBytes);

        Assert.Empty(decryptedBytes.ToArray());
    }

    [Fact]
    public void DecryptAesString_EmptyEncryptedByteArray_ReturnsEmptyString()
    {
        var text = string.Empty;
        var encryptedBytes = _service.EncryptAes(text);

        var decryptedText = _service.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void DecryptAesStringBase64_EmptyEncryptedBase64String_ReturnsEmptyString()
    {
        var text = string.Empty;
        var encryptedBase64 = _service.EncryptAesStringBase64(text);

        var decryptedText = _service.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }

    [Fact]
    public void DecryptAesToStream_ThrowsIfIvIsNotFound()
    {
        // IV should be 16 bytes
        var encryptedBytes = RandomNumberGenerator.GetBytes(8);
        var encryptedStream = new MemoryStream(encryptedBytes);

        // Exception should be thrown because the IV is not present
        Assert.Throws<InvalidOperationException>(() => _service.DecryptAesToStream(encryptedStream));
    }
}
