using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Model;
using Server.Services;

namespace ServerTests.ServiceTests;

public class FileServiceTests : BaseTests, IAsyncLifetime
{
    private readonly RefNotesContext _context;
    private readonly FileService _fileService;
    private readonly BrowserService _browserService;
    private readonly User _testUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly EncryptionService _encryptionService;
    private const string DirectoryPath = "/test";

    public FileServiceTests()
    {
        _encryptionService = new EncryptionService(AesKey, AesIv);
        _context = CreateDb();
        (_testUser, _claimsPrincipal) = CreateUser(_context, "test");
        _fileService = new FileService(_context, _encryptionService, AppConfig);
        _browserService = new BrowserService(_context, _encryptionService, AppConfig);
    }
    
    public async Task InitializeAsync()
    {
        await _browserService.AddDirectory(_claimsPrincipal, DirectoryPath);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddFile_AddsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        var encryptedPath = _encryptionService.EncryptAesStringBase64(DirectoryPath);
        var encryptedFileName = _encryptionService.EncryptAesStringBase64(fileName);

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == encryptedPath);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(encryptedFileName, directory.Files[0].Name);
    }

    [Fact]
    public async Task AddFile_ThrowsIfFileAlreadyExists()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName));
    }

    [Fact]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        await _fileService.DeleteFile(_claimsPrincipal, DirectoryPath, fileName);

        var encryptedPath = _encryptionService.EncryptAesStringBase64(DirectoryPath);

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == encryptedPath);

        Assert.NotNull(directory);
        Assert.Empty(directory.Files);
    }

    [Fact]
    public async Task DeleteFile_ThrowsIfFileDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileService.DeleteFile(_claimsPrincipal, DirectoryPath, fileName));
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsFilePath()
    {
        const string fileName = "testfile.txt";
        var addedFilePath = await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        var filePath = await _fileService.GetFilesystemFilePath(_claimsPrincipal, DirectoryPath, fileName);

        Assert.NotNull(filePath);
        Assert.Equal(addedFilePath, filePath);
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist()
    {
        var filePath = await _fileService.GetFilesystemFilePath(_claimsPrincipal, DirectoryPath, "nonexistent.txt");

        Assert.Null(filePath);
    }

    [Fact]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist()
    {
        const string nonExistentPath = "/nonexistent";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.GetFilesystemFilePath(_claimsPrincipal, nonExistentPath, "testfile.txt"));
    }

    [Fact]
    public async Task BrowserService_List_ReturnsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        var responseDirectory = await _browserService.List(_claimsPrincipal, DirectoryPath);

        Assert.NotNull(responseDirectory);
        Assert.Equal("test", responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files.FirstOrDefault()?.Name);
    }
}