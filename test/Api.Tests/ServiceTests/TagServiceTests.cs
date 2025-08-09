using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Api.Tests.Fixtures;
using Api.Tests.Mocks;
using Api.Utils;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class TagServiceTests : BaseTests
{
    private readonly TagService _service;
    private readonly IFileServiceUtils _fileServiceUtils;
    private readonly FakerResolver _fakerResolver;

    private readonly User _defaultUser;
    private readonly UserGroup _defaultGroup;

    public TagServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<TagService>().WithDb(dbFixture).WithFakers().WithFakeEncryption()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<TagService>();
        _fileServiceUtils = serviceProvider.GetRequiredService<IFileServiceUtils>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();

        _defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
        serviceProvider.GetRequiredService<IUserService>().GetCurrentUser().Returns(_defaultUser);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task ListAllTags_ListsTags(bool withGroup)
    {
        // Arrange
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var files = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate(2);
        var (file1, file2) = (files[0], files[1]);
        var tags1 = _fakerResolver.Get<FileTag>().ForFiles(file1).ForUserOrGroup(_defaultUser, group).Generate(2);
        var tags2 = _fakerResolver.Get<FileTag>().ForFiles(file2).ForUserOrGroup(_defaultUser, group).Generate(3);
        var tagNames = tags1.Concat(tags2).Select(t => t.Name).ToList();

        // Act
        var tags = await (withGroup ? _service.ListAllGroupTags(_defaultGroup.Id) : _service.ListAllTags());

        // Assert
        Assert.NotNull(tags);
        Assert.Equal(5, tags.Count);
        Assert.Equal(tagNames, tags);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task ListFileTags_ListsTags(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fakerResolver.Get<FileTag>().ForFiles(file).ForUserOrGroup(_defaultUser, group).Generate(3);
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, true).Returns((dir, file));

        var tags = await _service.ListFileTags(dir.Path, file.Name, group?.Id);

        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal(file.Tags.Select(t => t.Name), tags);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddFileTag_AddsTag(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, true).Returns((dir, file));
        const string tag = "test_tag";

        await _service.AddFileTag(dir.Path, file.Name, tag, group?.Id);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task AddFileTag_DoesntDoAnythingIfTagAlreadyExists(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, true).Returns((dir, file));
        const string tag = "test_tag";

        await _service.AddFileTag(dir.Path, file.Name, tag, group?.Id);
        await _service.AddFileTag(dir.Path, file.Name, tag, group?.Id);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.First().Name);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task RemoveFileTag_RemovesTag(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var file = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var tag = _fakerResolver.Get<FileTag>().ForFiles(file).ForUserOrGroup(_defaultUser, group).Generate();
        _fileServiceUtils.GetDirAndFile(dir.Path, file.Name, group?.Id, true).Returns((dir, file));

        await _service.RemoveFileTag(dir.Path, file.Name, tag.Name, group?.Id);

        Assert.NotNull(file);
        Assert.Empty(file.Tags);
    }
}
