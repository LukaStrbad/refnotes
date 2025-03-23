using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Services;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class TagServiceTests : BaseTests, IAsyncLifetime, IClassFixture<TestDatabaseFixture>
{
    private readonly RefNotesContext _context;
    private readonly FileService _fileService;
    private readonly TagService _tagService;
    private readonly BrowserService _browserService;
    private readonly User _testUser;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private const string DirectoryPath = "/tag_service_test";

    public TagServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        var encryptionService = new FakeEncryptionService();
        _context = testDatabaseFixture.Context;
        (_testUser, _claimsPrincipal) = CreateUser(_context, "test");
        _fileService = new FileService(_context, encryptionService, AppConfig);
        _tagService = new TagService(_context, encryptionService);
        _browserService = new BrowserService(_context, encryptionService);
    }

    public async Task InitializeAsync()
    {
        await _browserService.AddDirectory(_claimsPrincipal, DirectoryPath);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> AddFileWithTags(List<string> tags)
    {
        var fileName = $"{RandomString(16)}.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        var file = await _context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);

        file?.Tags.AddRange(tags.Select(t => new FileTag(t, _testUser.Id)));
        await _context.SaveChangesAsync();

        return fileName;
    }

    [Fact]
    public async Task ListAllTags_ListsTags()
    {
        await AddFileWithTags(["test_tag", "test_tag2"]);
        await AddFileWithTags(["test_tag3"]);

        var tags = await _tagService.ListAllTags(_claimsPrincipal);

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

        var tags = await _tagService.ListFileTags(_claimsPrincipal, DirectoryPath, fileName);

        Assert.NotNull(tags);
        Assert.Equal(2, tags.Count);
        Assert.Contains("test_tag", tags);
        Assert.Contains("test_tag2", tags);
    }

    [Fact]
    public async Task AddFileTag_AddsTag()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_claimsPrincipal, DirectoryPath, fileName, tag);

        var file = await _context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);

        Assert.NotNull(file);
        Assert.Single(file.Tags);
        Assert.Equal(tag, file.Tags.FirstOrDefault()?.Name);
    }

    [Fact]
    public async Task AddFileTag_DoesntDoAnythingIfTagAlreadyExists()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);

        const string tag = "test_tag";
        await _tagService.AddFileTag(_claimsPrincipal, DirectoryPath, fileName, tag);
        await _tagService.AddFileTag(_claimsPrincipal, DirectoryPath, fileName, tag);

        var file = await _context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);

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
            _tagService.AddFileTag(_claimsPrincipal, DirectoryPath, fileName, tag));
    }

    [Fact]
    public async Task RemoveFileTag_RemovesTag()
    {
        const string fileName = "testfile.txt";
        await _fileService.AddFile(_claimsPrincipal, DirectoryPath, fileName);
        const string tag = "test_tag";
        await _tagService.AddFileTag(_claimsPrincipal, DirectoryPath, fileName, tag);

        await _tagService.RemoveFileTag(_claimsPrincipal, DirectoryPath, fileName, tag);

        var file = await _context.Files
            .Include(f => f.Tags)
            .FirstOrDefaultAsync(f => f.Name == fileName);

        Assert.NotNull(file);
        Assert.Empty(file.Tags);
    }

    [Fact]
    public async Task RemoveFileTag_ThrowsIfFileDoesNotExist()
    {
        const string fileName = "testfile.txt";
        const string tag = "test_tag";

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _tagService.RemoveFileTag(_claimsPrincipal, DirectoryPath, fileName, tag));
    }
}