using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Exceptions;
using Server.Services;
using Server.Utils;
using ServerTests.Data;
using ServerTests.Data.Attributes;

namespace ServerTests.ServiceTests;

[ConcreteType<IFileServiceUtils, FileServiceUtils>]
[ConcreteType<IUserGroupService, UserGroupService>]
[ConcreteType<IEncryptionService, EncryptionService>]
public class BrowserServiceTests : BaseTests
{
    private readonly string _newDirectoryPath = $"/new_{RandomString(32)}";

    private static async Task<EncryptedDirectory?> GetDirectory(Sut<BrowserService> sut, string path,
        UserGroup? group)
    {
        var encryptedPath = sut.ServiceProvider.GetRequiredService<IEncryptionService>().EncryptAesStringBase64(path);
        if (group is null)
        {
            return await sut.Context.Directories.FirstOrDefaultAsync(
                d => d.Path == encryptedPath && d.OwnerId == sut.DefaultUser.Id,
                TestContext.Current.CancellationToken);
        }

        return await sut.Context.Directories.FirstOrDefaultAsync(d => d.Path == encryptedPath && d.GroupId == group.Id,
            TestContext.Current.CancellationToken);
    }

    [Theory, AutoData]
    public async Task AddRootDirectory_AddsDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory("/", group?.Id);

        var directory = await GetDirectory(sut, "/", group);
        Assert.NotNull(directory);
    }

    [Theory, AutoData]
    public async Task AddDirectoryToRoot_AddsDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);

        var directory = await GetDirectory(sut, _newDirectoryPath, group);
        Assert.NotNull(directory);
    }

    [Theory, AutoData]
    public async Task AddDirectoryToSubdirectory_AddsDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);

        var subPath = $"{_newDirectoryPath}/sub";
        await sut.Value.AddDirectory(subPath, group?.Id);

        var directory = await GetDirectory(sut, subPath, group);
        Assert.NotNull(directory);
    }

    [Theory, AutoData]
    public async Task AddDirectory_ThrowsIfDirectoryAlreadyExists(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);

        await Assert.ThrowsAsync<DirectoryAlreadyExistsException>(() =>
            sut.Value.AddDirectory(_newDirectoryPath, group?.Id));
    }

    [Theory, AutoData]
    public async Task DeleteDirectory_RemovesDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);

        await sut.Value.DeleteDirectory(_newDirectoryPath, group?.Id);

        var directory = await GetDirectory(sut, _newDirectoryPath, group);

        Assert.Null(directory);
    }

    [Theory, AutoData]
    public async Task DeleteDirectory_ThrowsIfDirectoryDoesNotExist(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            sut.Value.DeleteDirectory(_newDirectoryPath, group?.Id));
    }

    [Theory, AutoData]
    public async Task DeleteDirectory_ThrowsIfDirectoryNotEmpty(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);

        var subPath = $"{_newDirectoryPath}/sub";
        await sut.Value.AddDirectory(subPath, null);

        await Assert.ThrowsAsync<DirectoryNotEmptyException>(() =>
            sut.Value.DeleteDirectory(_newDirectoryPath, null));
    }

    [Theory, AutoData]
    public async Task List_ReturnsRootDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        var responseDirectory = await sut.Value.List(group?.Id);

        Assert.NotNull(responseDirectory);
        Assert.Equal("/", responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Theory, AutoData]
    public async Task List_ReturnsDirectory(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await sut.Value.AddDirectory(_newDirectoryPath, group?.Id);
        var expectedDirName = _newDirectoryPath.TrimStart('/');

        var rootDirectory = await sut.Value.List(group?.Id);
        Assert.NotNull(rootDirectory);
        Assert.Single(rootDirectory.Directories);
        Assert.Empty(rootDirectory.Files);
        Assert.Equal(expectedDirName, rootDirectory.Directories.FirstOrDefault());

        var responseDirectory = await sut.Value.List(group?.Id, _newDirectoryPath);

        Assert.NotNull(responseDirectory);
        Assert.Equal(expectedDirName, responseDirectory.Name);
        Assert.Empty(responseDirectory.Files);
        Assert.Empty(responseDirectory.Directories);
    }

    [Theory, AutoData]
    public async Task List_ReturnsNull_WhenDirectoryDoesNotExist(
        Sut<BrowserService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        var responseDirectory = await sut.Value.List(group?.Id, _newDirectoryPath);

        Assert.Null(responseDirectory);
    }
}