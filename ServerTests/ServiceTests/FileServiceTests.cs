using Server.Services;

namespace ServerTests.ServiceTests;

public class FileServiceTests : BaseTests
{
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        var encryptionService = new EncryptionService(AesKey, AesIv);
        _fileService = new FileService(encryptionService, AppConfig);
    }
    
    [Fact]
    public async Task SaveFile_CreatesEncryptedFile()
    {
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());

        await _fileService.SaveFile(fileName, inputStream);

        var filePath = Path.Combine(AppConfig.DataDir, fileName);
        Assert.True(File.Exists(filePath));
        var encryptedContent = await File.ReadAllTextAsync(filePath);
        Assert.NotEqual("test content", encryptedContent);
    }
    
    [Fact]
    public async Task SaveFile_ThrowsException_WhenFileNameIsEmpty()
    {
        var inputStream = new MemoryStream("test content"u8.ToArray());
        
        await Assert.ThrowsAsync<ArgumentException>(() => _fileService.SaveFile("", inputStream));
    }
    
    [Fact]
    public async Task GetFile_ReturnsDecryptedStream()
    {
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileService.SaveFile(fileName, inputStream);

        var stream = _fileService.GetFile(fileName);
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.Equal("test content", content);
    }
    
    [Fact]
    public async Task GetFile_ThrowsIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() => Task.FromResult(_fileService.GetFile(fileName)));
    }
    
    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        var inputStream = new MemoryStream("test content"u8.ToArray());
        await _fileService.SaveFile(fileName, inputStream);

        await _fileService.DeleteFile(fileName);

        var filePath = Path.Combine(AppConfig.DataDir, fileName);
        Assert.False(File.Exists(filePath));
    }
    
    [Fact]
    public async Task DeleteFile_DoesNothingIfFileNotFound()
    {
        const string fileName = "nonexistent.txt";

        await _fileService.DeleteFile(fileName);
    }
}