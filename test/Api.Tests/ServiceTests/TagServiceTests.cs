using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Mocks;
using Api.Utils;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
[ConcreteType<IFileServiceUtils, FileServiceUtils>]
[ConcreteType<IUserGroupService, UserGroupService>]
[ConcreteType<IFileService, FileService>]
[ConcreteType<IBrowserService, BrowserService>]
public class TagServiceTests : BaseTests
{
    private readonly string _directoryPath = $"/tag_service_test_{RandomString(32)}";

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task InitializeBaseDir(Sut<TagService> sut, UserGroup? group)
    {
        await sut.ServiceProvider.GetRequiredService<IBrowserService>()
            .AddDirectory(_directoryPath, group?.Id);
    }

    private static async Task<EncryptedFile?> GetFile(Sut<TagService> sut, string fileName)
    {
        return await sut.Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);
    }

    private async Task<string> AddFileWithTags(Sut<TagService> sut, List<string> tags, UserGroup? group)
    {
        var fileName = $"{RandomString(16)}.txt";
        await sut.ServiceProvider.GetRequiredService<IFileService>()
            .AddFile(_directoryPath, fileName, group?.Id);

        var file = await GetFile(sut, fileName);

        if (group is null)
        {
            file?.Tags.AddRange(tags.Select(t => new FileTag
            {
                Name = t,
                Owner = sut.DefaultUser
            }));
        }
        else
        {
            file?.Tags.AddRange(tags.Select(t => new FileTag
            {
                Name = t,
                GroupOwner = group
            }));
        }

        await sut.Context.SaveChangesAsync();

        return fileName;
    }

    [Theory, AutoData]
    public async Task ListAllTags_ListsTags(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await InitializeBaseDir(sut, group);

        await AddFileWithTags(sut, ["test_tag", "test_tag2"], group);
        await AddFileWithTags(sut, ["test_tag3"], group);

        var tags = await (group is null ? sut.Value.ListAllTags() : sut.Value.ListAllGroupTags(group.Id));

        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
        Assert.Contains("test_tag3", tags);
    }

    [Theory, AutoData]
    public async Task ListFileTags_ListsTags(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await InitializeBaseDir(sut, group);

        var fileName = await AddFileWithTags(sut, ["test_tag", "test_tag2"], group);

        var tags = await sut.Value.ListFileTags(_directoryPath, fileName, group?.Id);

        Assert.NotNull(tags);
        Assert.Equal(2, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
    }

    [Theory, AutoData]
    public async Task AddFileTag_AddsTag(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService)
    {
        await InitializeBaseDir(sut, group);

        var fileName = $"{RandomString(32)}.txt";
        await fileService.AddFile(_directoryPath, fileName, group?.Id);

        const string tag = "test_tag";
        await sut.Value.AddFileTag(_directoryPath, fileName, tag, group?.Id);

        var file = await GetFile(sut, fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Theory, AutoData]
    public async Task AddFileTag_DoesntDoAnythingIfTagAlreadyExists(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService)
    {
        await InitializeBaseDir(sut, group);

        var fileName = $"{RandomString(32)}.txt";
        await fileService.AddFile(_directoryPath, fileName, group?.Id);

        const string tag = "test_tag";
        await sut.Value.AddFileTag(_directoryPath, fileName, tag, group?.Id);
        await sut.Value.AddFileTag(_directoryPath, fileName, tag, group?.Id);

        var file = await GetFile(sut, fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Theory, AutoData]
    public async Task AddFileTag_ThrowsIfFileDoesNotExist(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await InitializeBaseDir(sut, group);

        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            sut.Value.AddFileTag(_directoryPath, fileName, tag, group?.Id));
    }

    [Theory, AutoData]
    public async Task RemoveFileTag_RemovesTag(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService)
    {
        await InitializeBaseDir(sut, group);

        const string fileName = "testfile.txt";
        await fileService.AddFile(_directoryPath, fileName, group?.Id);
        const string tag = "test_tag";
        await sut.Value.AddFileTag(_directoryPath, fileName, tag, group?.Id);

        await sut.Value.RemoveFileTag(_directoryPath, fileName, tag, group?.Id);

        var file = await GetFile(sut, fileName);

        Assert.NotNull(file);
        Assert.Empty(file.Tags);
    }

    [Theory, AutoData]
    public async Task RemoveFileTag_ThrowsIfFileDoesNotExist(
        Sut<TagService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group)
    {
        await InitializeBaseDir(sut, group);

        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            sut.Value.RemoveFileTag(_directoryPath, fileName, tag, group?.Id));
    }
}