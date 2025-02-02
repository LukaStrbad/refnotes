using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Services;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class FileServiceTests : BaseTests, IAsyncLifetime
{
    private readonly RefNotesContext _context;
    private readonly FileService _fileService;
    private readonly BrowserService _browserService;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private const string DirectoryPath = "/file_service_test";

    public FileServiceTests()
    {
        var encryptionService = new FakeEncryptionService();
        _context = CreateDb();
        (_, _claimsPrincipal) = CreateUser(_context, "test");
        _fileService = new FileService(_context, encryptionService, AppConfig);
        _browserService = new BrowserService(_context, encryptionService);
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

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == DirectoryPath);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(fileName, directory.Files[0].Name);
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
            _fileService.AddFile(_claimsPrincipal, "/nonexistent", fileName));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        await _fileService.DeleteFile(_claimsPrincipal, DirectoryPath, fileName);

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == DirectoryPath);

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
        // Remove the leading slash
        Assert.Equal(DirectoryPath[1..], responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files.FirstOrDefault()?.Name);
    }
}