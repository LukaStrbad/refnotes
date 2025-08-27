using Api.Exceptions;
using Api.Model;
using Api.Services;
using Api.Services.Files;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Api.Utils;
using Data.Model;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.ServiceTests;

public class FileServiceTests : BaseTests
{
    private readonly FakerResolver _fakerResolver;
    private readonly FileService _service;
    private readonly IFileServiceUtils _fileServiceUtils;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUserGroupService _userGroupService;

    private readonly User _defaultUser;
    private readonly UserGroup _defaultGroup;

    public FileServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<FileService>().WithDb(dbFixture).WithFakers().WithFakeEncryption()
            .CreateServiceProvider();
        _service = serviceProvider.GetRequiredService<FileService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        _fileServiceUtils = serviceProvider.GetRequiredService<IFileServiceUtils>();
        _fileStorageService = serviceProvider.GetRequiredService<IFileStorageService>();
        _userGroupService = serviceProvider.GetRequiredService<IUserGroupService>();

        _defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddFile_AddsFile(bool withGroup)
    {
        // Arrange
        const string fileName = "testfile.txt";
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fileServiceUtils.GetDirectory(dir.Path, true, group?.Id).Returns(dir);

        // Act
        await _service.AddFile(dir.Path, fileName, group?.Id);

        // Assert
        Assert.Single(dir.Files);
        Assert.Equal(fileName, dir.Files[0].Name);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddFile_ThrowsIfFileAlreadyExists(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileServiceUtils.GetDirectory(dir.Path, true, group?.Id).Returns(dir);

        // Act/Assert
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _service.AddFile(dir.Path, file.Name, group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddFile_ThrowsIfDirectoryDoesNotExist(bool withGroup)
    {
        // Arrange
        const string fileName = "testfile.txt";
        var group = withGroup ? _defaultGroup : null;

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _service.AddFile("/nonexistent", fileName, group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task MoveFile_MovesFile(bool withGroup)
    {
        // Arrange
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        var group = withGroup ? _defaultGroup : null;
        var dirs = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate(2);
        var (dir1, dir2) = (dirs[0], dirs[1]);
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir1).WithName(fileName).Generate();
        var originalTimestamp = file.Modified;
        _fileServiceUtils.GetDirAndFile(dir1.Path, fileName, group?.Id).Returns((dir1, file));
        _fileServiceUtils.GetDirectory(dir2.Path, true, group?.Id).Returns(dir2);

        // Act
        await _service.MoveFile($"{dir1.Path}/{fileName}", $"{dir2.Path}/{newFileName}", group?.Id);

        // Assert
        Assert.NotEqual(originalTimestamp, file.Modified);
        Assert.Empty(dir1.Files);
        Assert.NotEmpty(dir2.Files);
        Assert.Equal(newFileName, dir2.Files[0].Name);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task MoveFile_RenamesFile(bool withGroup)
    {
        // Arrange
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).WithName(fileName).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, fileName, group?.Id).Returns((dir, file));

        // Act
        await _service.MoveFile($"{dir.Path}/{fileName}", $"{dir.Path}/{newFileName}", group?.Id);

        // Assert
        Assert.Single(dir.Files);
        Assert.Equal(newFileName, dir.Files[0].Name);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task MoveFile_ThrowsExceptionIfNewDirectoryDoesntExist(bool withGroup)
    {
        // Arrange
        const string fileName = "testfile.txt";
        const string newFileName = "testfile2.txt";
        const string nonExistentDirectory = "/file_service_test_new_nonexistent";
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(dir).WithName(fileName).Generate();

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _service.MoveFile($"{dir.Path}/{fileName}", $"{nonExistentDirectory}/{newFileName}", group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task MoveFile_ThrowsExceptionIfFileAlreadyExists(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var files = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate(2);
        var (file1, file2) = (files[0], files[1]);
        _fileServiceUtils.GetDirAndFile(dir.Path, file1.Name, group?.Id).Returns((dir, file1));

        // Act/Assert
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() =>
            _service.MoveFile($"{dir.Path}/{file1.Name}", $"{dir.Path}/{file2.Name}", group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task DeleteFile_RemovesFile(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, false).Returns((dir, file));

        // Act
        await _service.DeleteFile(dir.Path, file.Name, group?.Id);

        // Assert
        Assert.Empty(dir.Files);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFilesystemFilePath_ReturnsFilePath(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id).Returns((dir, file));

        // Act
        var filePath = await _service.GetFilesystemFilePath(dir.Path, file.Name, group?.Id);

        // Assert
        Assert.NotNull(filePath);
        Assert.Equal(file.FilesystemName, filePath);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFilesystemFilePath_ReturnsNull_WhenFileDoesNotExist(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, "nonexistent.txt", group?.Id)
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var filePath = await _service.GetFilesystemFilePath(dir.Path, "nonexistent.txt", group?.Id);

        // Assert
        Assert.Null(filePath);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UpdateTimestamp_UpdatesTimestamp(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var oldTimestamp = file.Modified;
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id).Returns((dir, file));

        // Act
        await _service.UpdateTimestamp(dir.Path, file.Name, group?.Id);

        // Assert
        Assert.NotEqual(oldTimestamp, file.Modified);
        Assert.True(file.Modified > oldTimestamp);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFileInfo_ReturnsFileInfo(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileStorageService.GetFileSize(file.FilesystemName).Returns(1024L);
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, true).Returns((dir, file));

        // Act
        var fileInfo = await _service.GetFileInfo($"{dir.Path}/{file.Name}", group?.Id);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal($"{dir.Path}/{fileInfo.Name}", fileInfo.Path);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetFileInfoAsync_ReturnsFileInfo(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileStorageService.GetFileSize(file.FilesystemName).Returns(1024L);

        // Act
        var fileInfo = await _service.GetFileInfoAsync(file.Id);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal($"{dir.Path}/{file.Name}", fileInfo.Path);
        Assert.Equal(1024L, fileInfo.Size);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Created.Date);
        Assert.Equal(DateTime.UtcNow.Date, fileInfo.Modified.Date);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task GetEncryptedFileByRelativePathAsync_ReturnsRelativeFile(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dirFaker = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group);
        var dir1 = dirFaker.WithPath("/dir/subdir").Generate();
        var file1 = _fakerResolver.Get<EncryptedFile>().ForDir(dir1).Generate();
        var dir2 = dirFaker.WithPath("/dir/subdir2").Generate();
        var file2 = _fakerResolver.Get<EncryptedFile>().ForDir(dir2).Generate();
        var relativePath = $"../subdir2/{file2.Name}";

        // Act
        var relativeFile = await _service.GetEncryptedFileByRelativePathAsync(file1, relativePath);

        // Assert
        Assert.NotNull(relativeFile);
        Assert.Equal(file2.Id, relativeFile.Id);
    }

    [Fact]
    public async Task GetGroupDetailsFromFileIdAsync_ReturnsNull_ForUserFile()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        // Act
        var group = await _service.GetGroupDetailsFromFileIdAsync(file.Id);

        // Assert
        Assert.Null(group);
    }

    [Fact]
    public async Task GetGroupDetailsFromFileIdAsync_ReturnsGroupDetails_ForGroupFile()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForGroup(_defaultGroup).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _userGroupService.GetGroupDetailsAsync(_defaultGroup.Id)
            .Returns(new GroupDetails(_defaultGroup.Id, _defaultGroup.Name!));

        // Act
        var result = await _service.GetGroupDetailsFromFileIdAsync(file.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_defaultGroup.Id, result.Id);
        Assert.Equal(_defaultGroup.Name, result.Name);
    }

    [Fact]
    public async Task GetUserFromFile_ReturnsUser()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        // Act
        var user = await _service.GetUserFromFile(file);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(_defaultUser.Id, user.Id);
    }

    [Fact]
    public async Task GetUserGroupFromFile_ReturnsGroup()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForGroup(_defaultGroup).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        // Act
        var groupResult = await _service.GetUserGroupFromFile(file);

        // Assert
        Assert.NotNull(groupResult);
        Assert.Equal(_defaultGroup.Id, groupResult.Id);
    }

    [Fact]
    public async Task GetDirOwnerAsync_ReturnsUser_ForUserOwner()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        // Act
        var owner = await _service.GetDirOwnerAsync(file);

        // Assert
        Assert.Null(owner.Group);
        Assert.NotNull(owner.User);
        Assert.Equal(_defaultUser.Id, owner.User.Id);
    }

    [Fact]
    public async Task GetDirOwnerAsync_ReturnsGroup_ForGroupOwner()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForGroup(_defaultGroup).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();

        // Act
        var owner = await _service.GetDirOwnerAsync(file);

        // Assert
        Assert.Null(owner.User);
        Assert.NotNull(owner.Group);
        Assert.Equal(_defaultGroup.Id, owner.Group.Id);
    }

    [Fact]
    public async Task GetEncryptedFileForUserAsync_ReturnsFile()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var filePath = $"{dir.Path}/{file.Name}";

        // Act
        var fileResult = await _service.GetEncryptedFileForUserAsync(filePath, _defaultUser);

        // Assert
        Assert.NotNull(fileResult);
        Assert.Equal(file.Id, fileResult.Id);
    }

    [Fact]
    public async Task GetEncryptedFileForGroupAsync_ReturnsFile()
    {
        // Arrange
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForGroup(_defaultGroup).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var filePath = $"{dir.Path}/{file.Name}";

        // Act
        var fileResult = await _service.GetEncryptedFileForGroupAsync(filePath, _defaultGroup);

        // Asserts
        Assert.NotNull(fileResult);
        Assert.Equal(file.Id, fileResult.Id);
    }
}
