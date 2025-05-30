using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db.Model;
using Server.Exceptions;
using Server.Services;
using Server.Utils;
using ServerTests.Fixtures;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class FileServiceTests : BaseTests, IAsyncLifetime
{
    private readonly FileService _fileService;
    private readonly IFileStorageService _fileStorageService;
    private readonly BrowserService _browserService;
    private readonly string _directoryPath;
    private readonly string _newDirectoryPath;
    private readonly User _user;
    private UserGroup _group = null!;

    public FileServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        Context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_user, _) = CreateUser(Context, $"test_{rndString}");
        SetUser(_user);
        _fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(Context, encryptionService, UserService);
        var userGroupService = new UserGroupService(Context, encryptionService, UserService);
        _fileService = new FileService(Context, encryptionService, _fileStorageService, AppConfig, serviceUtils,
            UserService, userGroupService);
        _browserService = new BrowserService(Context, encryptionService, _fileStorageService, serviceUtils, UserService,
            userGroupService);

        rndString = RandomString(32);
        _directoryPath = $"/file_service_test_{rndString}";
        _newDirectoryPath = $"/file_service_test_new_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath, null);
        await _browserService.AddDirectory(_newDirectoryPath, null);

        _group = await CreateRandomGroup();
        await _browserService.AddDirectory(_directoryPath, _group.Id);
        await _browserService.AddDirectory(_newDirectoryPath, _group.Id);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task<EncryptedDirectory?> GetDirectory(string path, UserGroup? group = null)
    {
        if (group is null)
        {
            return await Context.Directories
                .Include(d => d.Files)
                .FirstOrDefaultAsync(d => d.Path == path && d.OwnerId == _user.Id,
                    TestContext.Current.CancellationToken);
        }

        return await Context.Directories
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Path == path && d.GroupId == group.Id,
                TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddFile_AddsFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        var directory = await GetDirectory(_directoryPath);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(fileName, directory.Files[0].Name);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddFile_AddsFile_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        var directory = await GetDirectory(_directoryPath, _group);

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
    [Trait("Category", "Group")]
    public async Task AddFile_ThrowsIfFileAlreadyExists_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.AddFile(_directoryPath, fileName, _group.Id));
    }

    [Fact]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.AddFile("/nonexistent", fileName, null));
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist_ForGroup()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.AddFile("/nonexistent", fileName, _group.Id));
    }

    [Fact]
    public async Task MoveFile_MovesFile()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName, null);

        var oldDirectory = await GetDirectory(_directoryPath);
        Assert.NotNull(oldDirectory);
        var file = oldDirectory.Files.FirstOrDefault(x => x.Name == fileName);
        Assert.NotNull(file);
        var timestamp = file.Modified;

        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_newDirectoryPath}/{newFileName}", null);

        // Re-fetch the old directory and file to ensure we have the latest data
        await Context.Entry(oldDirectory).ReloadAsync(TestContext.Current.CancellationToken);
        await Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        var newDirectory = await GetDirectory(_newDirectoryPath);

        Assert.NotEqual(timestamp, file.Modified);
        Assert.Empty(oldDirectory.Files);
        Assert.NotNull(newDirectory);
        Assert.NotEmpty(newDirectory.Files);
        Assert.Equal(newFileName, newDirectory.Files[0].Name);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task MoveFile_MovesFile_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        var oldDirectory = await GetDirectory(_directoryPath, _group);
        Assert.NotNull(oldDirectory);
        var file = oldDirectory.Files.FirstOrDefault(x => x.Name == fileName);
        Assert.NotNull(file);
        var timestamp = file.Modified;

        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_newDirectoryPath}/{newFileName}", _group.Id);

        // Re-fetch the old directory and file to ensure we have the latest data
        await Context.Entry(oldDirectory).ReloadAsync(TestContext.Current.CancellationToken);
        await Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        var newDirectory = await GetDirectory(_newDirectoryPath, _group);

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

        var directory = await GetDirectory(_directoryPath);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(newFileName, directory.Files[0].Name);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task MoveFile_RenamesFile_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await _fileService.AddFile(_directoryPath, fileName, _group.Id);
        await _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", _group.Id);

        var directory = await GetDirectory(_directoryPath, _group);

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
    [Trait("Category", "Group")]
    public async Task MoveFile_ThrowsExceptionIfNewDirectoryDoesntExist_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        const string nonExistentDirectory = "/file_service_test_new_nonexistent";

        await _fileService.AddFile(_directoryPath, fileName, _group.Id);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{nonExistentDirectory}/{newFileName}", _group.Id));
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
    [Trait("Category", "Group")]
    public async Task MoveFile_ThrowsExceptionIfFileAlreadyExists_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string newFileName = "testfile.txt";

        await _fileService.AddFile(_directoryPath, fileName, _group.Id);
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _fileService.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", _group.Id));
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        await _fileService.DeleteFile(_directoryPath, fileName, null);

        var directory = await GetDirectory(_directoryPath);

        Assert.NotNull(directory);
        Assert.Empty(directory.Files);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task DeleteFile_RemovesFile_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        await _fileService.DeleteFile(_directoryPath, fileName, _group.Id);

        var directory = await GetDirectory(_directoryPath, _group);

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
    [Trait("Category", "Group")]
    public async Task DeleteFile_ThrowsIfFileDoesNotExist_ForGroup()
    {
        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileService.DeleteFile(_directoryPath, fileName, _group.Id));
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
    [Trait("Category", "Group")]
    public async Task GetFilesystemFilePath_ReturnsFilePath_ForGroup()
    {
        const string fileName = "testfile.txt";
        var addedFilePath = await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, fileName, _group.Id);

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
    [Trait("Category", "Group")]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist_ForGroup()
    {
        var filePath = await _fileService.GetFilesystemFilePath(_directoryPath, "nonexistent.txt", _group.Id);

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
    [Trait("Category", "Group")]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist_ForGroup()
    {
        const string nonExistentPath = "/nonexistent";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileService.GetFilesystemFilePath(nonExistentPath, "testfile.txt", _group.Id));
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
    [Trait("Category", "Group")]
    public async Task BrowserService_List_ReturnsFile_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        var responseDirectory = await _browserService.List(_group.Id, _directoryPath);

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

        var dir = await GetDirectory(_directoryPath);
        Assert.NotNull(dir);
        Assert.NotEmpty(dir.Files);

        var file = dir.Files.FirstOrDefault(f => f.Name == fileName);
        Assert.NotNull(file);

        var oldTimestamp = file.Modified;

        // Ensure a sufficient delay to guarantee timestamp change
        // await Task.Delay(100, TestContext.Current.CancellationToken);
        await _fileService.UpdateTimestamp(_directoryPath, fileName, null);

        // Re-fetch the file to ensure we have the latest data
        await Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(file);
        Assert.NotEqual(oldTimestamp, file.Modified);
        Assert.True(file.Modified > oldTimestamp);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task UpdateTimestamp_UpdatesTimestamp_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        var dir = await GetDirectory(_directoryPath, _group);
        Assert.NotNull(dir);
        Assert.NotEmpty(dir.Files);

        var file = dir.Files.FirstOrDefault(f => f.Name == fileName);
        Assert.NotNull(file);

        var oldTimestamp = file.Modified;

        await _fileService.UpdateTimestamp(_directoryPath, fileName, _group.Id);

        // Re-fetch the file to ensure we have the latest data
        await Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

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

    [Fact]
    [Trait("Category", "Group")]
    public async Task GetFileInfo_ReturnsFileInfo_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        _fileStorageService.GetFileSize(Arg.Any<string>())
            .Returns(Task.FromResult(1024L));

        var fileInfo = await _fileService.GetFileInfo($"{_directoryPath}/{fileName}", _group.Id);

        Assert.NotNull(fileInfo);
        Assert.Equal(fileName, fileInfo.Name);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }
}
