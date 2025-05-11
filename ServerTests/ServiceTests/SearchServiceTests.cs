using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Server.Db;
using Server.Model;
using Server.Services;
using Server.Utils;
using ServerTests.Mocks;

namespace ServerTests.ServiceTests;

public class SearchServiceTests : BaseTests, IAsyncLifetime
{
    private readonly RefNotesContext _context;
    private readonly SearchService _searchService;
    private readonly BrowserService _browserService;
    private readonly FileService _fileService;
    private readonly TagService _tagService;
    private readonly string _directoryPath;

    public SearchServiceTests(TestDatabaseFixture testDatabaseFixture)
    {
        _context = testDatabaseFixture.CreateContext();

        var encryptionService = new FakeEncryptionService();
        var fileStorageService = Substitute.For<IFileStorageService>();
        var cache = new MemoryCache();
        var rndString = RandomString(32);
        var (_, claimsPrincipal) = CreateUser(_context, $"test_{rndString}");
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var serviceUtils = new ServiceUtils(_context, encryptionService, cache, httpContextAccessor);
        _searchService = new SearchService(_context, encryptionService, fileStorageService, serviceUtils, cache);
        _browserService = new BrowserService(_context, encryptionService, fileStorageService, serviceUtils);
        _fileService = new FileService(_context, encryptionService, fileStorageService, AppConfig, serviceUtils);
        var userGroupService = new UserGroupService(_context, encryptionService, serviceUtils);
        _tagService = new TagService(_context, encryptionService, userGroupService, serviceUtils);

        rndString = RandomString(32);
        _directoryPath = $"/search_service_test_{rndString}";
    }

    public async ValueTask InitializeAsync()
    {
        await _browserService.AddDirectory(_directoryPath, null);
    }

    public ValueTask DisposeAsync()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SearchFiles_SearchesFilesByName()
    {
        await _fileService.AddFile(_directoryPath, "foo.txt", null);
        await _fileService.AddFile(_directoryPath, "bar.txt", null);
        await _fileService.AddFile(_directoryPath, "baz.txt", null);
        await _fileService.AddFile(_directoryPath, "foo2.txt", null);

        var options = new SearchOptionsDto("foo", 0, 100);

        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/foo.txt", filePaths);
        Assert.Contains($"{_directoryPath}/foo2.txt", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByTags()
    {
        await _fileService.AddFile(_directoryPath, "foo.txt", null);
        await _fileService.AddFile(_directoryPath, "bar.txt", null);
        await _fileService.AddFile(_directoryPath, "baz.txt", null);

        await _tagService.AddFileTag(_directoryPath, "foo.txt", "tag1", null);
        await _tagService.AddFileTag(_directoryPath, "bar.txt", "tag2", null);
        await _tagService.AddFileTag(_directoryPath, "baz.txt", "tag2", null);
        await _tagService.AddFileTag(_directoryPath, "baz.txt", "tag3", null);

        var options = new SearchOptionsDto("", 0, 100, Tags: ["tag2"]);

        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/bar.txt", filePaths);
        Assert.Contains($"{_directoryPath}/baz.txt", filePaths);
    }

    [Fact]
    public async Task SearchFiles_NoSearchTermOrFilters_ReturnsAllFiles()
    {
        await _fileService.AddFile(_directoryPath, "foo.txt", null);
        await _fileService.AddFile(_directoryPath, "bar.md", null);
        await _fileService.AddFile(_directoryPath, "baz.pdf", null);

        var options = new SearchOptionsDto("", 0, 100);
        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(3, filePaths.Count);
        Assert.Contains($"{_directoryPath}/foo.txt", filePaths);
        Assert.Contains($"{_directoryPath}/bar.md", filePaths);
        Assert.Contains($"{_directoryPath}/baz.pdf", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByType()
    {
        await _fileService.AddFile(_directoryPath, "one.md", null);
        await _fileService.AddFile(_directoryPath, "two.txt", null);
        await _fileService.AddFile(_directoryPath, "three.pdf", null);

        var options = new SearchOptionsDto("", 0, 100, FileTypes: ["txt", ".pdf"]);
        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Contains($"{_directoryPath}/two.txt", filePaths);
        Assert.Contains($"{_directoryPath}/three.pdf", filePaths);
        Assert.Equal(2, filePaths.Count);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByModifiedDateRange()
    {
        await _fileService.AddFile(_directoryPath, "date1.txt", null);
        await _fileService.AddFile(_directoryPath, "date2.txt", null);
        await _fileService.AddFile(_directoryPath, "date3.txt", null);

        // Set file modified dates using direct DbContext modification (simulate file modified dates)
        var dbDirectory = await _context.Directories
            .Where(dir => dir.Path == _directoryPath)
            .Include(dir => dir.Files)
            .FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(dbDirectory);

        var dbFiles = dbDirectory.Files.ToList();

        var file1Modified = DateTime.Parse("2025-01-01");
        var file2Modified = DateTime.Parse("2025-02-01");
        var file3Modified = DateTime.Parse("2025-03-01");

        dbFiles.Find(f => f.Name == "date1.txt")!.Modified = file1Modified;
        dbFiles.Find(f => f.Name == "date2.txt")!.Modified = file2Modified;
        dbFiles.Find(f => f.Name == "date3.txt")!.Modified = file3Modified;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // This should return all files between "2025-01-15" and "2025-02-15"
        var options = new SearchOptionsDto("", 0, 100,
            ModifiedFrom: DateTime.Parse("2025-01-15"), ModifiedTo: DateTime.Parse("2025-02-15"));

        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Single(filePaths);
        Assert.Contains($"{_directoryPath}/date2.txt", filePaths);


        // This should return all files after ModifiedFrom date
        options = options with { ModifiedTo = null };

        files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/date2.txt", filePaths);
        Assert.Contains($"{_directoryPath}/date3.txt", filePaths);
    }


    [Fact]
    public async Task SearchFiles_UnmatchedCriteria_ReturnsEmpty()
    {
        await _fileService.AddFile(_directoryPath, "abc.txt", null);
        await _fileService.AddFile(_directoryPath, "def.md", null);

        var options = new SearchOptionsDto("xyz", 0, 100, FileTypes: ["pdf"], Tags: ["notag"]);
        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(files);
    }

    [Fact]
    public async Task SearchFiles_ComplexFiltering_OnlyCorrectFileReturned()
    {
        await _fileService.AddFile(_directoryPath, "special1.md", null);
        await _fileService.AddFile(_directoryPath, "special2.md", null);
        await _tagService.AddFileTag(_directoryPath, "special1.md", "projectA", null);

        var dbFile = _context.Files.First(f => f.Name == "special1.md");
        dbFile.Modified = DateTime.UtcNow.AddDays(-5);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new SearchOptionsDto(
            SearchTerm: "special1",
            Page: 0,
            PageSize: 100,
            Tags: ["projectA"],
            FileTypes: ["md"],
            ModifiedFrom: DateTime.UtcNow.AddDays(-10),
            ModifiedTo: DateTime.UtcNow.AddDays(-1)
        );
        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Single(filePaths);
        Assert.Contains($"{_directoryPath}/special1.md", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FileName_MatchingIsCaseInsensitive()
    {
        await _fileService.AddFile(_directoryPath, "FOO.TXT", null);
        var options = new SearchOptionsDto("foo", 0, 100);

        var files = await _searchService.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(files);
        Assert.EndsWith("FOO.TXT", files[0].Path);
    }
}