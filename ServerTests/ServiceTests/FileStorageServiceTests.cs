using Server.Services;

namespace ServerTests.ServiceTests;

public class FileStorageServiceTests : BaseTests
{
    private readonly FileStorageService _fileStorageService;

    public FileStorageServiceTests()
    {
        var encryptionService = new EncryptionService(AesKey, AesIv);
        _fileStorageService = new FileStorageService(encryptionService, AppConfig);
    }

    [Fact]
    public async Task SaveFile_CreatesEncryptedFile()
    {
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());

        await _fileStorageService.SaveFileAsync(fileName, inputStream);

        var filePath = Path.Combine(AppConfig.DataDir, fileName);
        Assert.True(File.Exists(filePath));
        var encryptedContent = await File.ReadAllTextAsync(filePath);
        Assert.NotEqual("test content", encryptedContent);
    }

    [Fact]
    public async Task SaveFile_ThrowsException_WhenFileNameIsEmpty()
    {
        var inputStream = new MemoryStream("test content"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(() => _fileStorageService.SaveFileAsync("", inputStream));
    }

    [Fact]
    public async Task GetFile_ReturnsDecryptedStream()
    {
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileStorageService.SaveFileAsync(fileName, inputStream);

        var stream = _fileStorageService.GetFile(fileName);
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
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileStorageService.SaveFileAsync(fileName, inputStream);

        await _fileStorageService.DeleteFile(fileName);

        var filePath = Path.Combine(AppConfig.DataDir, fileName);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFile_DoesNothingIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await _fileStorageService.DeleteFile(fileName);
    }
}
