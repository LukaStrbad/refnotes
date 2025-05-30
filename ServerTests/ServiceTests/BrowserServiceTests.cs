using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Services;
using Server.Utils;
using ServerTests.Fixtures;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class BrowserServiceTests : BaseTests
{
    private readonly BrowserService _browserService;
    private readonly User _user;
    private readonly User _secondUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly EncryptionService _encryptionService;

    private readonly string _newDirectoryPath;

    public BrowserServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        _encryptionService = new EncryptionService(AesKey, AesIv);
        Context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_user, _claimsPrincipal) = CreateUser(Context, $"test_{rndString}");
        SetUser(_user);
        (_secondUser, _) = CreateUser(Context, $"test_second_{rndString}");
        var fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(Context, _encryptionService, UserService);
        var userGroupService = new UserGroupService(Context, _encryptionService, UserService);
        _browserService =
            new BrowserService(Context, _encryptionService, fileStorageService, serviceUtils, UserService,
                userGroupService);

        rndString = RandomString(32);
        _newDirectoryPath = $"/new_{rndString}";
    }

    private async Task<EncryptedDirectory?> GetDirectory(string path, UserGroup? group = null)
    {
        var encryptedPath = _encryptionService.EncryptAesStringBase64(path);
        if (group is null)
        {
            return await Context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath && d.OwnerId == _user.Id,
                TestContext.Current.CancellationToken);
        }

        return await Context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath && d.GroupId == group.Id,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AddRootDirectory_AddsDirectory()
    {
        await _browserService.AddDirectory("/", null);

        var directory = await GetDirectory("/");
        Assert.NotNull(directory);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddRootDirectory_AddsDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory("/", group.Id);

        var directory = GetDirectory("/", group);
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectoryToRoot_AddsDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var directory = await GetDirectory(_newDirectoryPath);
        Assert.NotNull(directory);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task AddDirectoryToRoot_AddsDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);

        var directory = await GetDirectory(_newDirectoryPath, group);
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectoryToSubdirectory_AddsDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, null);
        
        var directory = await GetDirectory(subPath);
        Assert.NotNull(directory);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task AddDirectoryToSubdirectory_AddsDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, group.Id);
        
        var directory = await GetDirectory(subPath, group);
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectory_ThrowsIfDirectoryAlreadyExists()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        await Assert.ThrowsAsync<DirectoryAlreadyExistsException>(() =>
            _browserService.AddDirectory(_newDirectoryPath, null));
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task AddDirectory_ThrowsIfDirectoryAlreadyExists_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);

        await Assert.ThrowsAsync<DirectoryAlreadyExistsException>(() =>
            _browserService.AddDirectory(_newDirectoryPath, group.Id));
    }

    [Fact]
    public async Task DeleteDirectory_RemovesDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        await _browserService.DeleteDirectory(_newDirectoryPath, null);

        var directory = await Context.Directories.FirstOrDefaultAsync(
            d => d.Path == _encryptionService.EncryptAesStringBase64(_newDirectoryPath),
            TestContext.Current.CancellationToken);
        Assert.Null(directory);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task DeleteDirectory_RemovesDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);

        await _browserService.DeleteDirectory(_newDirectoryPath, group.Id);

        var directory = await GetDirectory(_newDirectoryPath, group);
        Assert.Null(directory);
    }

    [Fact]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, null));
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist_ForGroup()
    {
        var group = await CreateRandomGroup();
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, group.Id));
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, null);

        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, null));
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, group.Id);

        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, group.Id));
    }

    [Fact]
    public async Task List_ReturnsRootDirectory()
    {
        var responseDirectory = await _browserService.List(null);

        Assert.NotNull(responseDirectory);
        Assert.Equal("/", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task List_ReturnsRootDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        var responseDirectory = await _browserService.List(group.Id);

        Assert.NotNull(responseDirectory);
        Assert.Equal("/", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Fact]
    public async Task List_ReturnsDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);
        var expectedDirName = _newDirectoryPath.TrimStart('/');

        var rootDirectory = await _browserService.List(null);
        Assert.NotNull(rootDirectory);
        Assert.Single(rootDirectory.Directories);
        Assert.Empty(rootDirectory.Files);
        Assert.Equal(expectedDirName, rootDirectory.Directories.FirstOrDefault());

        var responseDirectory = await _browserService.List(null, _newDirectoryPath);

        Assert.NotNull(responseDirectory);
        Assert.Equal(expectedDirName, responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task List_ReturnsDirectory_ForGroup()
    {
        var group = await CreateRandomGroup();
        await _browserService.AddDirectory(_newDirectoryPath, group.Id);
        var expectedDirName = _newDirectoryPath.TrimStart('/');

        var rootDirectory = await _browserService.List(group.Id);
        Assert.NotNull(rootDirectory);
        Assert.Single(rootDirectory.Directories);
        Assert.Empty(rootDirectory.Files);
        Assert.Equal(expectedDirName, rootDirectory.Directories.FirstOrDefault());

        var responseDirectory = await _browserService.List(group.Id, _newDirectoryPath);

        Assert.NotNull(responseDirectory);
        Assert.Equal(expectedDirName, responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Fact]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        var responseDirectory = await _browserService.List(null, _newDirectoryPath);

        Assert.Null(responseDirectory);
    }
    
    [Fact]
    [Trait("Category", "Group")]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist_ForGroup()
    {
        var group = await CreateRandomGroup();
        var responseDirectory = await _browserService.List(group.Id, _newDirectoryPath);

        Assert.Null(responseDirectory);
    }
}