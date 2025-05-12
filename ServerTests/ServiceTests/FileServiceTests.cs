using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
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
    private readonly IFileStorageService _fileStorageService;
    private readonly BrowserService _browserService;
    private readonly string _directoryPath;
    private readonly string _newDirectoryPath;

    public FileServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        _context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        var (user, _) = CreateUser(_context, $"test_{rndString}");
        var userService = Substitute.For<IUserService>();
        userService.GetUser().Returns(user);
        _fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(_context, encryptionService, userService);
        _fileService = new FileService(_context, encryptionService, _fileStorageService, AppConfig, serviceUtils);
        _browserService = new BrowserService(_context, encryptionService, _fileStorageService, serviceUtils, userService);

        rndString = RandomString(32);
        _directoryPath = $"/file_service_test_{rndString}";
        _newDirectoryPath = $"/file_service_test_new_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath, null);
        await _browserService.AddDirectory(_newDirectoryPath, null);
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
        await _fileService.AddFile(_directoryPath, fileName, null);

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
        await _fileService.AddFile(_directoryPath, fileName, null);

        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.AddFile(_directoryPath, fileName, null));
    }

    [Fact]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.AddFile("/nonexistent", fileName, null));
    }

    [Fact]
    public async Task MoveFile_MovesFile()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName, null);

        var oldDirectory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);
        Assert.NotNull(oldDirectory);
        var file = oldDirectory.Files.FirstOrDefault(x => x.Name == fileName);
        Assert.NotNull(file);
        var timestamp = file.Modified;

        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_newDirectoryPath}/{newFileName}", null);

        // Re-fetch the old directory and file to ensure we have the latest data
        await _context.Entry(oldDirectory).ReloadAsync(TestContext.Current.CancellationToken);
        await _context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        var newDirectory = await _context.Directories
            .Include(x => x.Files)
            .FirstOrDefaultAsync(d => d.Path == _newDirectoryPath, TestContext.Current.CancellationToken);

        Assert.NotEqual(timestamp, file.Modified);
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

        await _fileService.AddFile(_directoryPath, fileName, null);
        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", null);

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

        await _fileService.AddFile(_directoryPath, fileName, null);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{nonExistentDirectory}/{newFileName}", null));
    }

    [Fact]
    public async Task MoveFile_ThrowsExceptionIfFileAlreadyExists()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile.txt";

        await _fileService.AddFile(_directoryPath, fileName, null);
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", null));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        await _fileService.DeleteFile(_directoryPath, fileName, null);

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
            _fileService.DeleteFile(_directoryPath, fileName, null));
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsFilePath()
    {
        const string fileName = "testfile.txt";
        var addedFilePath = await _fileService.AddFile(_directoryPath, fileName, null);

        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, fileName, null);

        Assert.NotNull(filePath);
        Assert.Equal(addedFilePath, filePath);
    }

    [Fact]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist()
    {
        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, "nonexistent.txt", null);

        Assert.Null(filePath);
    }

    [Fact]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist()
    {
        const string nonExistentPath = "/nonexistent";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.GetFilesystemFilePath(nonExistentPath, "testfile.txt", null));
    }

    [Fact]
    public async Task BrowserService_List_ReturnsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        var responseDirectory = await _browserService.List(null, _directoryPath);

        Assert.NotNull(responseDirectory);
        // Remove the leading slash
        Assert.Equal(_directoryPath[1..], responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files.FirstOrDefault()?.Name);
    }

    [Fact]
    public async Task UpdateTimestamp_UpdatesTimestamp()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        var dir = await _context.Directories
            .Include(encryptedDirectory => encryptedDirectory.Files)
            .FirstOrDefaultAsync(d => d.Path == _directoryPath, TestContext.Current.CancellationToken);
        Assert.NotNull(dir);
        Assert.NotEmpty(dir.Files);

        var file = dir.Files.FirstOrDefault(f => f.Name == fileName);
        Assert.NotNull(file);

        var oldTimestamp = file.Modified;

        // Ensure a sufficient delay to guarantee timestamp change
        // await Task.Delay(100, TestContext.Current.CancellationToken);
        await _fileService.UpdateTimestamp(_directoryPath, fileName, null);

        // Re-fetch the file to ensure we have the latest data
        await _context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(file);
        Assert.NotEqual(oldTimestamp, file.Modified);
        Assert.True(file.Modified > oldTimestamp);
    }

    [Fact]
    public async Task GetFileInfo_ReturnsFileInfo()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        _fileStorageService.GetFileSize(Arg.Any<string>())
            .Returns(Task.FromResult(1024L));

        var fileInfo = await _fileService.GetFileInfo($"{_directoryPath}/{fileName}", null);

        Assert.NotNull(fileInfo);
        Assert.Equal(fileName, fileInfo.Name);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }
}