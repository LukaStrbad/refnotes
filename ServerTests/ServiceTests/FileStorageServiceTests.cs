using Server.Services;
using ServerTests.Mocks;
using Xunit.Abstractions;

namespace ServerTests.ServiceTests;

public class FileStorageServiceTests : BaseTests
{
    private readonly FileStorageService _fileStorageService;
    private readonly string _fileName;

    public FileStorageServiceTests()
    {
        var encryptionService = new FakeEncryptionService();
        _fileStorageService = new FileStorageService(encryptionService, AppConfig);
        _fileName = $"{RandomString(16)}.txt";
    }

    [Fact]
    public async Task SaveFile_CreatesEncryptedFile()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());

        await _fileStorageService.SaveFileAsync(_fileName, inputStream);

        var filePath = Path.Combine(AppConfig.DataDir, _fileName);
        Assert.True(File.Exists(filePath));
        var encryptedContent = await File.ReadAllTextAsync(filePath);
        // We are not testing the encryption algorithm here, so we don't need to decrypt the content
        Assert.Equal("test content", encryptedContent);
    }

    [Fact]
    public async Task SaveFile_ThrowsException_WhenFileNameIsEmpty()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() => _fileStorageService.SaveFileAsync("", inputStream));
    }

    [Fact]
    public async Task GetFile_ReturnsDecryptedStream()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileStorageService.SaveFileAsync(_fileName, inputStream);

        await using var stream = _fileStorageService.GetFile(_fileName);
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.Equal("test content", content);
    }

    [Fact]
    public async Task GetFile_ThrowsIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() => Task.FromResult(_fileStorageService.GetFile(fileName)));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        await using var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileStorageService.SaveFileAsync(_fileName, inputStream);

        await _fileStorageService.DeleteFile(_fileName);

        var filePath = Path.Combine(AppConfig.DataDir, _fileName);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFile_DoesNothingIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await _fileStorageService.DeleteFile(fileName);
    }
}
