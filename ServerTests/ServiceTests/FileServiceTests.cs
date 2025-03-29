using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Services;
using Server.Utils;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class FileServiceTests : BaseTests, IAsyncLifetime
{
    private readonly RefNotesContext _context;
    private readonly FileService _fileService;
    private readonly BrowserService _browserService;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly string _directoryPath;
    private readonly string _newDirectoryPath;

    public FileServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        _context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        var cache = new MemoryCache();
        (_, _claimsPrincipal) = CreateUser(_context, $"test_{rndString}");
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = _claimsPrincipal }
        };
        var serviceUtils = new ServiceUtils(_context, encryptionService, cache, httpContextAccessor);
        _fileService = new FileService(_context, encryptionService, AppConfig, serviceUtils);
        _browserService = new BrowserService(_context, encryptionService, serviceUtils);

        rndString = RandomString(32);
        _directoryPath = $"/file_service_test_{rndString}";
        _newDirectoryPath = $"/file_service_test_new_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath);
        await _browserService.AddDirectory(_newDirectoryPath);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AddFile_AddsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName);

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(fileName, directory.Files[0].Name);
    }

    [Fact]
    public async Task AddFile_ThrowsIfFileAlreadyExists()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName);

        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.AddFile(_directoryPath, fileName));
    }

    [Fact]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.AddFile("/nonexistent", fileName));
    }

    [Fact]
    public async Task MoveFile_MovesFile()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName);
        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_newDirectoryPath}/{newFileName}");

        var oldDirectory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);

        var newDirectory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _newDirectoryPath, TestContext.Current.CancellationToken);

        Assert.NotNull(oldDirectory);
        Assert.Empty(oldDirectory.Files);
        Assert.NotNull(newDirectory);
        Assert.NotEmpty(newDirectory.Files);
        Assert.Equal(newFileName, newDirectory.Files[0].Name);
    }

    [Fact]
    public async Task MoveFile_RenamesFile()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName);
        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}");

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(newFileName, directory.Files[0].Name);
    }

    [Fact]
    public async Task MoveFile_ThrowsExceptionIfNewDirectoryDoesntExist()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        const string nonExistentDirectory = "/file_service_test_new_nonexistent";

        await _fileService.AddFile(_directoryPath, fileName);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{nonExistentDirectory}/{newFileName}"));
    }

    [Fact]
    public async Task MoveFile_ThrowsExceptionIfFileAlreadyExists()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile.txt";

        await _fileService.AddFile(_directoryPath, fileName);
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}"));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName);

        await _fileService.DeleteFile(_directoryPath, fileName);

        var directory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);

        Assert.NotNull(directory);
        Assert.Empty(directory.Files);
    }

    [Fact]
    public async Task DeleteFile_ThrowsIfFileDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileService.DeleteFile(_directoryPath, fileName));
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsFilePath()
    {
        const string fileName = "testfile.txt";
        var addedFilePath = await _fileService.AddFile(_directoryPath, fileName);

        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, fileName);

        Assert.NotNull(filePath);
        Assert.Equal(addedFilePath, filePath);
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist()
    {
        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, "nonexistent.txt");

        Assert.Null(filePath);
    }

    [Fact]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist()
    {
        const string nonExistentPath = "/nonexistent";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.GetFilesystemFilePath(nonExistentPath, "testfile.txt"));
    }

    [Fact]
    public async Task BrowserService_List_ReturnsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName);

        var responseDirectory = await _browserService.List(_directoryPath);

        Assert.NotNull(responseDirectory);
        // Remove the leading slash
        Assert.Equal(_directoryPath[1..], responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files.FirstOrDefault()?.Name);
    }
}