using Api.Exceptions;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Api.Utils;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class DirectoryServiceTests : BaseTests
{
    private readonly DirectoryService _directoryService;
    private readonly RefNotesContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly string _newDirectoryPath = $"/new_{RandomString(32)}";
    private readonly User _defaultUser;
    private readonly UserGroup _defaultGroup;
    private readonly FakerResolver _fakerResolver;
    private readonly IFileServiceUtils _fileServiceUtils;

    public DirectoryServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<DirectoryService>()
            .WithDb(dbFixture)
            .WithFakers()
            .WithFakeEncryption()
            .CreateServiceProvider();

        _directoryService = serviceProvider.GetRequiredService<DirectoryService>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        var userService = serviceProvider.GetRequiredService<IUserService>();
        _fileServiceUtils = serviceProvider.GetRequiredService<IFileServiceUtils>();
        var userGroupService = serviceProvider.GetRequiredService<IUserGroupService>();

        // Setup default user and group
        _defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
        _fakerResolver.Get<UserGroupRole>().ForUser(_defaultUser).ForGroup(_defaultGroup).Generate();
        userService.GetCurrentUser().Returns(_defaultUser);
        userGroupService.GetGroupAsync(_defaultGroup.Id).Returns(_defaultGroup);
    }

    private async Task<EncryptedDirectory?> GetDirectory(string path, UserGroup? group)
    {
        var pathHash = _encryptionService.HashString(path);
        if (group is null)
        {
            return await _context.Directories.FirstOrDefaultAsync(
                d => d.PathHash == pathHash && d.OwnerId == _defaultUser.Id,
                TestContext.Current.CancellationToken);
        }

        return await _context.Directories.FirstOrDefaultAsync(d => d.PathHash == pathHash && d.GroupId == group.Id,
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddRootDirectory_AddsDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;

        // Act
        await _directoryService.AddDirectory("/", group?.Id);

        // Assert
        var directory = await GetDirectory("/", group);
        Assert.NotNull(directory);
    }
    
    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddRootDirectory_Throws_IfDirectoryAlreadyExists(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var existingDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath("/").Generate();
        _fileServiceUtils.GetDirectory("/", false, group?.Id).Returns(existingDir);

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryAlreadyExistsException>(() =>
            _directoryService.AddDirectory("/", group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddDirectoryToRoot_AddsDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var rootDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath("/").Generate();
        _fileServiceUtils.GetDirectory("/", false, group?.Id).Returns(rootDir);

        // Act
        await _directoryService.AddDirectory(_newDirectoryPath, group?.Id);

        // Assert
        var directory = await GetDirectory(_newDirectoryPath, group);
        Assert.NotNull(directory);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddDirectoryToSubdirectory_AddsDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var existingDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fileServiceUtils.GetDirectory(existingDir.Path, false, group?.Id).Returns(existingDir);

        // Act
        var subPath = $"{existingDir.Path}/sub";
        await _directoryService.AddDirectory(subPath, group?.Id);

        // Assert
        var directory = await GetDirectory(subPath, group);
        Assert.NotNull(directory);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddDirectory_ThrowsIfDirectoryAlreadyExists(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var existingDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        _fileServiceUtils.GetDirectory(existingDir.Path, false, group?.Id).Returns(existingDir);

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryAlreadyExistsException>(() =>
            _directoryService.AddDirectory(existingDir.Path, group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task DeleteDirectory_RemovesDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var parentDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath("/").Generate();
        var existingDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithParent(parentDir).Generate();
        _fileServiceUtils.GetDirectory(existingDir.Path, false, group?.Id).Returns(existingDir);

        // Act
        await _directoryService.DeleteDirectory(existingDir.Path, group?.Id);

        // Assert
        var directory = await GetDirectory(existingDir.Path, group);
        Assert.Null(directory);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _directoryService.DeleteDirectory(_newDirectoryPath, group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath(_newDirectoryPath).Generate();
        _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithParent(dir).Generate(); // Subdir

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() =>
            _directoryService.DeleteDirectory(_newDirectoryPath, group?.Id));
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task List_ReturnsRootDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var rootDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath("/").Generate();
        _fileServiceUtils.GetDirectory("/", true, group?.Id).Returns(rootDir);

        // Act
        var responseDirectory = await _directoryService.List(group?.Id);

        // Assert
        Assert.NotNull(responseDirectory);
        Assert.Equal("/", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task List_ReturnsDirectory(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var rootDir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithPath("/").Generate();
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).WithParent(rootDir).Generate();
        var expectedDirName = Path.GetFileName(dir.Path);
        _fileServiceUtils.GetDirectory("/", true, group?.Id).Returns(rootDir);
        _fileServiceUtils.GetDirectory(dir.Path, true, group?.Id).Returns(dir);

        // Act
        var rootDirectory = await _directoryService.List(group?.Id);
        var responseDirectory = await _directoryService.List(group?.Id, dir.Path);

        // Assert - Root directory
        Assert.NotNull(rootDirectory);
        Assert.Single(rootDirectory.Directories);
        Assert.Empty(rootDirectory.Files);
        Assert.Equal(expectedDirName, rootDirectory.Directories.FirstOrDefault());
        // Assert - New directory
        Assert.NotNull(responseDirectory);
        Assert.Equal(expectedDirName, responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;

        // Act
        var responseDirectory = await _directoryService.List(group?.Id, _newDirectoryPath);

        // Assert
        Assert.Null(responseDirectory);
    }
}
