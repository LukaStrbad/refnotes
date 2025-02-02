using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Exceptions;
using Server.Services;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class TagServiceTests : BaseTests, IAsyncLifetime
{
    private readonly RefNotesContext _context;
    private readonly FileService _fileService;
    private readonly TagService _tagService;
    private readonly BrowserService _browserService;
    private readonly ClaimsPrincipal _claimsPrincipal;
    private const string DirectoryPath = "/tag_service_test";

    public TagServiceTests()
    {
        var encryptionService = new FakeEncryptionService();
        _context = CreateDb();
        (_, _claimsPrincipal) = CreateUser(_context, "test");
        _fileService = new FileService(_context, encryptionService, AppConfig);
        _tagService = new TagService(_context, encryptionService);
        _browserService = new BrowserService(_context, encryptionService);
    }

    public async Task InitializeAsync()
    {
        await _browserService.AddDirectory(_claimsPrincipal, DirectoryPath);
    }

    public Task DisposeAsync() => Task.CompletedTask;

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