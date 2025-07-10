using Api.Exceptions;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Api.Tests.Mocks;
using Api.Utils;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
[ConcreteType<IFileServiceUtils, FileServiceUtils>]
[ConcreteType<IUserGroupService, UserGroupService>]
[ConcreteType<IBrowserService, BrowserService>]
public class FileServiceTests : BaseTests
{
    private readonly string _directoryPath;
    private readonly string _newDirectoryPath;

    public FileServiceTests()
    {
        var rndString = RandomString(32);
        _directoryPath = $"/file_service_test_{rndString}";
        _newDirectoryPath = $"/file_service_test_new_{rndString}";
    }

    private static async Task<EncryptedDirectory?> GetDirectory(Sut<FileService> sut, string path,
        UserGroup? group)
    {
        var encryptedPath = sut.ServiceProvider.GetRequiredService<IEncryptionService>().EncryptAesStringBase64(path);
        if (group is null)
        {
            return await sut.Context.Directories
                .Include(d => d.Files)
                .FirstOrDefaultAsync(
                    d => d.Path == encryptedPath && d.OwnerId == sut.DefaultUser.Id,
                    TestContext.Current.CancellationToken);
        }

        return await sut.Context.Directories
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Path == encryptedPath && d.GroupId == group.Id,
                TestContext.Current.CancellationToken);
    }

    private async Task CreateDirectories(Sut<FileService> sut, UserGroup? group)
    {
        var browserService = sut.ServiceProvider.GetRequiredService<IBrowserService>();
        await browserService.AddDirectory(_directoryPath, group?.Id);
        await browserService.AddDirectory(_newDirectoryPath, group?.Id);
    }

    [Theory, AutoData]
    public async Task AddFile_AddsFile(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        var directory = await GetDirectory(sut, _directoryPath, group);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(fileName, directory.Files[0].Name);
    }

    [Theory, AutoData]
    public async Task AddFile_ThrowsIfFileAlreadyExists(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            sut.Value.AddFile(_directoryPath, fileName, group?.Id));
    }

    [Theory, AutoData]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            sut.Value.AddFile("/nonexistent", fileName, group?.Id));
    }

    [Theory, AutoData]
    public async Task MoveFile_MovesFile
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        var oldDirectory = await GetDirectory(sut, _directoryPath, group);
        Assert.NotNull(oldDirectory);
        var file = oldDirectory.Files.FirstOrDefault(x => x.Name == fileName);
        Assert.NotNull(file);
        var timestamp = file.Modified;

        await sut.Value.MoveFile($"{_directoryPath}/{fileName}", $"{_newDirectoryPath}/{newFileName}", group?.Id);

        // Re-fetch the old directory and file to ensure we have the latest data
        await sut.Context.Entry(oldDirectory).ReloadAsync(TestContext.Current.CancellationToken);
        await sut.Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        var newDirectory = await GetDirectory(sut, _newDirectoryPath, group);

        Assert.NotEqual(timestamp, file.Modified);
        Assert.Empty(oldDirectory.Files);
        Assert.NotNull(newDirectory);
        Assert.NotEmpty(newDirectory.Files);
        Assert.Equal(newFileName, newDirectory.Files[0].Name);
    }

    [Theory, AutoData]
    public async Task MoveFile_RenamesFile
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";

        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);
        await sut.Value.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", group?.Id);

        var directory = await GetDirectory(sut, _directoryPath, group);

        Assert.NotNull(directory);
        Assert.Single(directory.Files);
        Assert.Equal(newFileName, directory.Files[0].Name);
    }

    [Theory, AutoData]
    public async Task MoveFile_ThrowsExceptionIfNewDirectoryDoesntExist
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        const string nonExistentDirectory = "/file_service_test_new_nonexistent";

        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            sut.Value.MoveFile($"{_directoryPath}/{fileName}", $"{nonExistentDirectory}/{newFileName}", group?.Id));
    }

    [Theory, AutoData]
    public async Task MoveFile_ThrowsExceptionIfFileAlreadyExists
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        const string newFileName = "testfile.txt";

        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            sut.Value.MoveFile($"{_directoryPath}/{fileName}", $"{_directoryPath}/{newFileName}", group?.Id));
    }

    [Theory, AutoData]
    public async Task DeleteFile_RemovesFile
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        await sut.Value.DeleteFile(_directoryPath, fileName, group?.Id);

        var directory = await GetDirectory(sut, _directoryPath, group);

        Assert.NotNull(directory);
        Assert.Empty(directory.Files);
    }

    [Theory, AutoData]
    public async Task DeleteFile_ThrowsIfFileDoesNotExist
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            sut.Value.DeleteFile(_directoryPath, fileName, group?.Id));
    }

    [Theory, AutoData]
    public async Task GetFilesystemFilePath_ReturnsFilePath
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        var addedFilePath = await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        var filePath = await sut.Value.GetFilesystemFilePath(_directoryPath, fileName, group?.Id);

        Assert.NotNull(filePath);
        Assert.Equal(addedFilePath, filePath);
    }

    [Theory, AutoData]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        var filePath = await sut.Value.GetFilesystemFilePath(_directoryPath, "nonexistent.txt", group?.Id);

        Assert.Null(filePath);
    }

    [Theory, AutoData]
    public async Task GetFilesystemPath_ThrowsIfDirectoryDoesNotExist
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string nonExistentPath = "/nonexistent";
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            sut.Value.GetFilesystemFilePath(nonExistentPath, "testfile.txt", group?.Id));
    }

    [Theory, AutoData]
    public async Task BrowserService_List_ReturnsFile(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IBrowserService browserService)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        var responseDirectory = await browserService.List(group?.Id, _directoryPath);

        Assert.NotNull(responseDirectory);
        // Remove the leading slash
        Assert.Equal(_directoryPath[1..], responseDirectory.Name);
        Assert.Single(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
        Assert.Equal(fileName, responseDirectory.Files.FirstOrDefault()?.Name);
    }

    [Theory, AutoData]
    public async Task UpdateTimestamp_UpdatesTimestamp
    (Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        var dir = await GetDirectory(sut, _directoryPath, group);
        Assert.NotNull(dir);
        Assert.NotEmpty(dir.Files);

        var file = dir.Files.FirstOrDefault(f => f.Name == fileName);
        Assert.NotNull(file);

        var oldTimestamp = file.Modified;

        // Ensure a sufficient delay to guarantee timestamp change
        // await Task.Delay(100, TestContext.Current.CancellationToken);
        await sut.Value.UpdateTimestamp(_directoryPath, fileName, group?.Id);

        // Re-fetch the file to ensure we have the latest data
        await sut.Context.Entry(file).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(file);
        Assert.NotEqual(oldTimestamp, file.Modified);
        Assert.True(file.Modified > oldTimestamp);
    }

    [Theory, AutoData]
    public async Task GetFileInfo_ReturnsFileInfo(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await CreateDirectories(sut, group);

        const string fileName = "testfile.txt";
        await sut.Value.AddFile(_directoryPath, fileName, group?.Id);

        sut.ServiceProvider.GetRequiredService<IFileStorageService>()
            .GetFileSize(Arg.Any<string>())
            .Returns(Task.FromResult(1024L));
        // fileStorageService.GetFileSize(Arg.Any<string>())
        //     .Returns(Task.FromResult(1024L));

        var fileInfo = await sut.Value.GetFileInfo($"{_directoryPath}/{fileName}", group?.Id);

        Assert.NotNull(fileInfo);
        Assert.Equal($"{_directoryPath}/{fileName}", fileInfo.Path);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }

    [Theory, AutoData]
    public async Task GetFileInfoAsync_ReturnsFileInfo(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IBrowserService browserService,
        IFileStorageService fileStorageService)
    {
        await CreateDirectories(sut, group);
        var subdirectoryPath = $"{_directoryPath}/subdir";
        const string fileName = "testfile.txt";
        var filePath = $"{subdirectoryPath}/{fileName}";

        // Create subdirectory
        await browserService.AddDirectory(subdirectoryPath, group?.Id);

        await sut.Value.AddFile(subdirectoryPath, fileName, group?.Id);
        fileStorageService.GetFileSize(Arg.Any<string>())
            .Returns(Task.FromResult(1024L));

        var encryptedFile = await sut.Value.GetEncryptedFileAsync(filePath, group?.Id);
        Assert.NotNull(encryptedFile);

        var fileInfo = await sut.Value.GetFileInfoAsync(encryptedFile.Id);

        Assert.NotNull(fileInfo);
        Assert.Equal(filePath, fileInfo.Path);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }

    [Theory, AutoData]
    public async Task GetEncryptedFileByRelativePathAsync_ReturnsRelativeFile(
        Sut<FileService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        EncryptedFileFakerImplementation fileFaker,
        EncryptedDirectoryFakerImplementation dirFaker)
    {
        var dir1 = dirFaker.CreateFaker()
            .WithPath("/dir/subdir").ForUserOrGroup(sut.DefaultUser, group).Generate();
        var dir2 = dirFaker.CreateFaker()
            .WithPath("/dir/subdir2").ForUserOrGroup(sut.DefaultUser, group).Generate();

        var file1 = fileFaker.CreateFaker().ForDir(dir1).Generate();
        var file2 = fileFaker.CreateFaker().ForDir(dir2).Generate();

        var relativePath = $"../subdir2/{file2.Name}";

        var relativeFile = await sut.Value.GetEncryptedFileByRelativePathAsync(file1, relativePath);

        Assert.NotNull(relativeFile);
        Assert.Equal(file2.Id, relativeFile.Id);
    }

    [Theory, AutoData]
    public async Task GetGroupDetailsFromFileIdAsync_ReturnsNull_ForUserFile(
        Sut<FileService> sut,
        EncryptedFileFakerImplementation fileFaker,
        EncryptedDirectoryFakerImplementation dirFaker)
    {
        var dir = dirFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();
        var file = fileFaker.CreateFaker().ForDir(dir).Generate();

        var group = await sut.Value.GetGroupDetailsFromFileIdAsync(file.Id);

        Assert.Null(group);
    }

    [Theory, AutoData]
    public async Task GetGroupDetailsFromFileIdAsync_ReturnsGroupDetails_ForGroupFile(
        Sut<FileService> sut,
        [FixtureGroup] UserGroup group,
        EncryptedFileFakerImplementation fileFaker,
        EncryptedDirectoryFakerImplementation dirFaker)
    {
        var dir = dirFaker.CreateFaker().ForGroup(group).Generate();
        var file = fileFaker.CreateFaker().ForDir(dir).Generate();

        var result = await sut.Value.GetGroupDetailsFromFileIdAsync(file.Id);

        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal(group.Name, result.Name);
    }
}
