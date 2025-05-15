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

public class TagServiceTests : BaseTests, IAsyncLifetime
{
    private readonly FileService _fileService;
    private readonly TagService _tagService;
    private readonly BrowserService _browserService;
    private readonly User _user;
    private readonly User _secondUser;
    private readonly string _directoryPath;
    private UserGroup _group = null!;

    public TagServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        Context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_user, _) = CreateUser(Context, $"test_{rndString}");
        SetUser(_user);
        (_secondUser, _) = CreateUser(Context, $"test_second_{rndString}");
        

        var fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(Context, encryptionService, UserService);
        var userGroupService = new UserGroupService(Context, encryptionService, UserService);
        _fileService = new FileService(Context, encryptionService, fileStorageService, AppConfig, serviceUtils,
            UserService, userGroupService);
        _tagService = new TagService(Context, encryptionService, userGroupService, serviceUtils, UserService);
        _browserService =
            new BrowserService(Context, encryptionService, fileStorageService, serviceUtils, UserService,
                userGroupService);

        rndString = RandomString(32);
        _directoryPath = $"/tag_service_test_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath, null);

        _group = await CreateRandomGroup();
        await _browserService.AddDirectory(_directoryPath, _group.Id);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task<EncryptedFile?> GetFile(string fileName)
    {
        return await Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);
    }

    private async Task<string> AddFileWithTags(List<string> tags, UserGroup? group = null)
    {
        var fileName = $"{RandomString(16)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, group?.Id);

        var file = await GetFile(fileName);

        if (group is null)
        {
            file?.Tags.AddRange(tags.Select(t => new FileTag
            {
                Name = t,
                Owner = _user
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

        await Context.SaveChangesAsync();

        return fileName;
    }

    [Fact]
    public async Task ListAllTags_ListsTags()
    {
        await AddFileWithTags(["test_tag", "test_tag2"]);
        await AddFileWithTags(["test_tag3"]);

        var tags = await _tagService.ListAllTags();

        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
        Assert.Contains("test_tag3", tags);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task ListAllGroupTags_ListsTags()
    {
        await AddFileWithTags(["test_tag", "test_tag2"], _group);
        await AddFileWithTags(["test_tag3"], _group);

        var tags = await _tagService.ListAllGroupTags(_group.Id);

        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
        Assert.Contains("test_tag3", tags);
    }

    [Fact]
    public async Task ListFileTags_ListsTags()
    {
        var fileName = await AddFileWithTags(["test_tag", "test_tag2"]);

        var tags = await _tagService.ListFileTags(_directoryPath, fileName, null);

        Assert.NotNull(tags);
        Assert.Equal(2, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task ListFileTags_ListsTags_ForGroup()
    {
        var fileName = await AddFileWithTags(["test_tag", "test_tag2"], _group);

        var tags = await _tagService.ListFileTags(_directoryPath, fileName, _group.Id);

        Assert.NotNull(tags);
        Assert.Equal(2, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
    }

    [Fact]
    public async Task AddFileTag_AddsTag()
    {
        var fileName = $"{RandomString(32)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddFileTag_AddsTag_ForGroup()
    {
        var fileName = $"{RandomString(32)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Fact]
    public async Task AddFileTag_DoesntDoAnythingIfTagAlreadyExists()
    {
        var fileName = $"{RandomString(32)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddFileTag_DoesntDoAnythingIfTagAlreadyExists_ForGroup()
    {
        var fileName = $"{RandomString(32)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id);
        await _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Fact]
    public async Task AddFileTag_ThrowsIfFileDoesNotExist()
    {
        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _tagService.AddFileTag(_directoryPath, fileName, tag, null));
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task AddFileTag_ThrowsIfFileDoesNotExist_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id));
    }

    [Fact]
    public async Task RemoveFileTag_RemovesTag()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);
        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);

        await _tagService.RemoveFileTag(_directoryPath, fileName, tag, null);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Empty(file.Tags);
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task RemoveFileTag_RemovesTag_ForGroup()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, _group.Id);
        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id);

        await _tagService.RemoveFileTag(_directoryPath, fileName, tag, _group.Id);

        var file = await GetFile(fileName);

        Assert.NotNull(file);
        Assert.Empty(file.Tags);
    }

    [Fact]
    public async Task RemoveFileTag_ThrowsIfFileDoesNotExist()
    {
        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _tagService.RemoveFileTag(_directoryPath, fileName, tag, null));
    }

    [Fact]
    [Trait("Category", "Group")]
    public async Task RemoveFileTag_ThrowsIfFileDoesNotExist_ForGroup()
    {
        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _tagService.RemoveFileTag(_directoryPath, fileName, tag, _group.Id));
    }

    [Fact]
    [Trait("Category", "Permissions")]
    public async Task ListAllGroupTags_ThrowsForbiddenException_WhenUserIsNotInGroup()
    {
        SetUser(_secondUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _tagService.ListAllGroupTags(_group.Id));
    }

    [Fact]
    [Trait("Category", "Permissions")]
    public async Task ListFileTags_ThrowsForbiddenException_WhenUserIsNotInGroup()
    {
        const string fileName = "test_permissions.txt";
        SetUser(_secondUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _tagService.ListFileTags(_directoryPath, fileName, _group.Id));
    }

    [Fact]
    [Trait("Category", "Permissions")]
    public async Task AddFileTag_ThrowsForbiddenException_WhenUserIsNotInGroup()
    {
        const string fileName = "test_permissions.txt";
        const string tag = "test_tag";
        SetUser(_secondUser);
        
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _tagService.AddFileTag(_directoryPath, fileName, tag, _group.Id));
    }

    [Fact]
    [Trait("Category", "Permissions")]
    public async Task RemoveFileTag_ThrowsForbiddenException_WhenUserIsNotInGroup()
    {
        const string fileName = "test_permissions.txt";
        const string tag = "test_tag";
        SetUser(_secondUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _tagService.RemoveFileTag(_directoryPath, fileName, tag, _group.Id));
    }
}