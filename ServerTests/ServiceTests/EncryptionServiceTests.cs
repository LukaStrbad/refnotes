using Server;
using Server.Services;
using ServerTests.Data;
using ServerTests.Extensions;

namespace ServerTests.ServiceTests;

using System.Text;
using Xunit;

public class EncryptionServiceTests : BaseTests
{
    [Theory, AutoData]
    public void EncryptAes_EncryptsByteArray_ReturnsEncryptedByteArray(
        Sut<EncryptionService> sut)
    {
        var bytes = "test data"u8.ToArray();

        var encryptedBytes = sut.Value.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
    }

    [Theory, AutoData]
    public async Task EncryptAes_EncryptsStream_ReturnsEncryptedStream(Sut<EncryptionService> sut)
    {
        var bytes = "test data"u8.ToArray();
        await using var inputStream = new MemoryStream(bytes);
        await using var outputStream = new MemoryStream();

        await sut.Value.EncryptAesToStreamAsync(inputStream, outputStream);

        var encryptedBytes = outputStream.ToArray();
        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(bytes, encryptedBytes);
    }

    [Theory, AutoData]
    public void EncryptAes_EncryptsString_ReturnsEncryptedByteArray(Sut<EncryptionService> sut)
    {
        const string text = "test data";

        var encryptedBytes = sut.Value.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEqual(Encoding.UTF8.GetBytes(text), encryptedBytes);
    }

    [Theory, AutoData]
    public void EncryptAesStringBase64_EncryptsString_ReturnsBase64String(Sut<EncryptionService> sut)
    {
        const string text = "test data";

        var encryptedBase64 = sut.Value.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEqual(text, encryptedBase64);
    }

    [Theory, AutoData]
    public void DecryptAes_DecryptsEncryptedByteArray_ReturnsOriginalByteArray(Sut<EncryptionService> sut)
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = sut.Value.EncryptAes(bytes);

        var decryptedBytes = sut.Value.DecryptAes(encryptedBytes);

        Assert.Equal(bytes, decryptedBytes);
    }

    [Theory, AutoData]
    public void DecryptAes_DecryptsEncryptedStream_ReturnsOriginalStream(Sut<EncryptionService> sut)
    {
        var bytes = "test data"u8.ToArray();
        var encryptedBytes = sut.Value.EncryptAes(bytes);

        using var encryptedInputStream = new MemoryStream(encryptedBytes);

        using var outputStream = sut.Value.DecryptAesToStream(encryptedInputStream);

        var decryptedBytes = outputStream.ToArray();
        Assert.Equal(bytes, decryptedBytes);
    }

    [Theory, AutoData]
    public void DecryptAesString_DecryptsEncryptedByteArray_ReturnsOriginalString(Sut<EncryptionService> sut)
    {
        const string text = "test data";
        var encryptedBytes = sut.Value.EncryptAes(text);

        var decryptedText = sut.Value.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Theory, AutoData]
    public void DecryptAesStringBase64_DecryptsEncryptedBase64String_ReturnsOriginalString(Sut<EncryptionService> sut)
    {
        const string text = "test data";
        var encryptedBase64 = sut.Value.EncryptAesStringBase64(text);

        var decryptedText = sut.Value.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }

    [Theory, AutoData]
    public void EncryptAes_EmptyByteArray_ReturnsNonEmptyEncryptedByteArray(Sut<EncryptionService> sut)
    {
        var bytes = Array.Empty<byte>();

        var encryptedBytes = sut.Value.EncryptAes(bytes);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Theory, AutoData]
    public void EncryptAes_EmptyString_ReturnsNonEmptyEncryptedByteArray(Sut<EncryptionService> sut)
    {
        var text = string.Empty;

        var encryptedBytes = sut.Value.EncryptAes(text);

        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
    }

    [Theory, AutoData]
    public void EncryptAesStringBase64_EmptyString_ReturnsNonEmptyBase64String(Sut<EncryptionService> sut)
    {
        var text = string.Empty;

        var encryptedBase64 = sut.Value.EncryptAesStringBase64(text);

        Assert.NotNull(encryptedBase64);
        Assert.NotEmpty(encryptedBase64);
    }

    [Theory, AutoData]
    public void DecryptAes_EmptyEncryptedByteArray_ReturnsEmptyByteArray(Sut<EncryptionService> sut)
    {
        var bytes = Array.Empty<byte>();
        var encryptedBytes = sut.Value.EncryptAes(bytes);

        var decryptedBytes = sut.Value.DecryptAes(encryptedBytes);

        Assert.Empty(decryptedBytes);
    }

    [Theory, AutoData]
    public void DecryptAesString_EmptyEncryptedByteArray_ReturnsEmptyString(Sut<EncryptionService> sut)
    {
        var text = string.Empty;
        var encryptedBytes = sut.Value.EncryptAes(text);

        var decryptedText = sut.Value.DecryptAesString(encryptedBytes);

        Assert.Equal(text, decryptedText);
    }

    [Theory, AutoData]
    public void DecryptAesStringBase64_EmptyEncryptedBase64String_ReturnsEmptyString(Sut<EncryptionService> sut)
    {
        var text = string.Empty;
        var encryptedBase64 = sut.Value.EncryptAesStringBase64(text);

        var decryptedText = sut.Value.DecryptAesStringBase64(encryptedBase64);

        Assert.Equal(text, decryptedText);
    }
}