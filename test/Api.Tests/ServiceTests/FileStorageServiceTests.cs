using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Mocks;
using Api;

namespace Api.Tests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
public class FileStorageServiceTests : BaseTests
{
    private readonly string _fileName = $"{RandomString(32)}.txt";

    [Theory, AutoData]
    public async Task SaveFile_CreatesEncryptedFile(Sut<FileStorageService> sut, AppConfiguration appConfig)
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());

        await sut.Value.SaveFileAsync(_fileName, inputStream);

        var filePath = Path.Combine(appConfig.DataDir, _fileName);
        Assert.True(File.Exists(filePath));
        var encryptedContent = await File.ReadAllTextAsync(filePath, TestContext.Current.CancellationToken);
        // We are not testing the encryption algorithm here, so we don't need to decrypt the content
        Assert.Equal("test content", encryptedContent);
    }

    [Theory, AutoData]
    public async Task SaveFile_ThrowsException_WhenFileNameIsEmpty(Sut<FileStorageService> sut)
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.Value.SaveFileAsync("", inputStream));
    }

    [Theory, AutoData]
    public async Task GetFile_ReturnsDecryptedStream(Sut<FileStorageService> sut)
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        await sut.Value.SaveFileAsync(_fileName, inputStream);

        await using var stream = sut.Value.GetFile(_fileName);

        using var sr = new StreamReader(stream);
        var content = await sr.ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Equal("test content", content);
    }

    [Theory, AutoData]
    public async Task GetFile_ThrowsIfFileNotFound(Sut<FileStorageService> sut)
    {
        const string fileName = "nonexistent.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() => Task.FromResult(sut.Value.GetFile(fileName)));
    }

    [Theory, AutoData]
    public async Task DeleteFile_RemovesFile(Sut<FileStorageService> sut)
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        await sut.Value.SaveFileAsync(_fileName, inputStream);

        await sut.Value.DeleteFile(_fileName);

        var filePath = Path.Combine(TestFolder, _fileName);
        Assert.False(File.Exists(filePath));
    }

    [Theory, AutoData]
    public async Task DeleteFile_DoesNothingIfFileNotFound(Sut<FileStorageService> sut)
    {
        const string fileName = "nonexistent.txt";

        await sut.Value.DeleteFile(fileName);
    }

    [Theory, AutoData]
    public async Task GetFileSize_ReturnsFileSize(Sut<FileStorageService> sut)
    {
        var fileContent = "test content"u8.ToArray();
        await using var inputStream = new MemoryStream(fileContent);
        await sut.Value.SaveFileAsync(_fileName, inputStream);

        var fileSize = await sut.Value.GetFileSize(_fileName);

        Assert.Equal(fileContent.LongLength, fileSize);
    }
}