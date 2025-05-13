using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db;
using Server.Db.Model;
using Server.Services;
using Server.Utils;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class TagServiceTests : BaseTests, IAsyncLifetime
{
    private readonly FileService _fileService;
    private readonly TagService _tagService;
    private readonly BrowserService _browserService;
    private readonly User _testUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private readonly string _directoryPath;

    public TagServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        Context = testDatabaseFixture.CreateContext();
        var rndString = RandomString(32);
        (_testUser, _claimsPrincipal) = CreateUser(Context, $"test_{rndString}");
        SetUser(_testUser);

        var fileStorageService = Substitute.For<IFileStorageService>();
        var serviceUtils = new FileServiceUtils(Context, encryptionService, UserService);
        var userGroupService = new UserGroupService(Context, encryptionService, UserService);
        _fileService = new FileService(Context, encryptionService, fileStorageService, AppConfig, serviceUtils, UserService, userGroupService);
        _tagService = new TagService(Context, encryptionService, userGroupService, serviceUtils, UserService);
        _browserService =
            new BrowserService(Context, encryptionService, fileStorageService, serviceUtils, UserService, userGroupService);

        rndString = RandomString(32);
        _directoryPath = $"/tag_service_test_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath, null);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task<string> AddFileWithTags(List<string> tags)
    {
        var fileName = $"{RandomString(16)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        var file = await Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);

        file?.Tags.AddRange(tags.Select(t => new FileTag
        {
            Name = t,
            Owner = _testUser
        }));
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
    public async Task AddFileTag_AddsTag()
    {
        var fileName = $"{RandomString(32)}.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);

        var file = await Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName, TestContext.Current.CancellationToken);

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

        var file = await Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName, TestContext.Current.CancellationToken);

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
    public async Task RemoveFileTag_RemovesTag()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_directoryPath, fileName, null);
        const string tag = "test_tag";
        await _tagService.AddFileTag(_directoryPath, fileName, tag, null);

        await _tagService.RemoveFileTag(_directoryPath, fileName, tag, null);

        var file = await Context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName, TestContext.Current.CancellationToken);

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
}