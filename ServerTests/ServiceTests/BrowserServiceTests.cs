using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;
using Server.Services;
using Server.Utils;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class BrowserServiceTests : BaseTests
{
    private readonly RefNotesContext _context;
    private readonly BrowserService _browserService;
    private readonly User _testUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly EncryptionService _encryptionService;

    private readonly string _newDirectoryPath;

    public BrowserServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        _encryptionService = new EncryptionService(AesKey, AesIv);
        _context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_testUser, _claimsPrincipal) = CreateUser(_context, $"test_{rndString}");
        var userService = Substitute.For<IUserService>();
        userService.GetUser().Returns(_testUser);
        var fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(_context, _encryptionService, userService);
        _browserService =
            new BrowserService(_context, _encryptionService, fileStorageService, serviceUtils, userService);

        rndString = RandomString(32);
        _newDirectoryPath = $"/new_{rndString}";
    }

    [Fact]
    public async Task AddRootDirectory_AddsDirectory()
    {
        await _browserService.AddDirectory("/", null);

        var directory = await _context.Directories.FirstOrDefaultAsync(
            d => d.Path == _encryptionService.EncryptAesStringBase64("/"), TestContext.Current.CancellationToken);
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectoryToRoot_AddsDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var encryptedPath = _encryptionService.EncryptAesStringBase64(_newDirectoryPath);
        var directory =
            await _context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath,
                TestContext.Current.CancellationToken);
        Assert.NotNull(directory);
    }

    [Fact]
    public async Task AddDirectoryToSubdirectory_AddsDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, null);

        var encryptedPath = _encryptionService.EncryptAesStringBase64(subPath);
        var directory =
            await _context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath,
                TestContext.Current.CancellationToken);
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
    public async Task DeleteDirectory_RemovesDirectory()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        await _browserService.DeleteDirectory(_newDirectoryPath, null);

        var directory = await _context.Directories.FirstOrDefaultAsync(
            d => d.Path == _encryptionService.EncryptAesStringBase64(_newDirectoryPath),
            TestContext.Current.CancellationToken);
        Assert.Null(directory);
    }

    [Fact]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, null));
    }

    [Fact]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty()
    {
        await _browserService.AddDirectory(_newDirectoryPath, null);

        var subPath = $"{_newDirectoryPath}/sub";
        await _browserService.AddDirectory(subPath, null);

        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() =>
            _browserService.DeleteDirectory(_newDirectoryPath, null));
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
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist()
    {
        var responseDirectory = await _browserService.List(null, _newDirectoryPath);

        Assert.Null(responseDirectory);
    }
}