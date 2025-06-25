using Api.Model;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Mocks;
using Api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Api_Tests_Mocks_MemoryCache = Api.Tests.Mocks.MemoryCache;
using MemoryCache = Api.Tests.Mocks.MemoryCache;
using Mocks_MemoryCache = Api.Tests.Mocks.MemoryCache;
using Tests_Mocks_MemoryCache = Api.Tests.Mocks.MemoryCache;

namespace Api.Tests.ServiceTests;

[ConcreteType<IEncryptionService, FakeEncryptionService>]
[ConcreteType<IFileServiceUtils, FileServiceUtils>]
[ConcreteType<IFileService, FileService>]
[ConcreteType<IBrowserService, BrowserService>]
[ConcreteType<ITagService, TagService>]
[ConcreteType<IMemoryCache, Api_Tests_Mocks_MemoryCache>]
public class SearchServiceTests : BaseTests
{
    private readonly string _directoryPath = $"/search_service_test_{RandomString(32)}";

    private async Task CreateBaseDirectory(Sut<SearchService> sut)
    {
        await sut.ServiceProvider.GetRequiredService<IBrowserService>()
            .AddDirectory(_directoryPath, null);
    }

    [Theory, AutoData]
    public async Task SearchFiles_SearchesFilesByName(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "foo.txt", null);
        await fileService.AddFile(_directoryPath, "bar.txt", null);
        await fileService.AddFile(_directoryPath, "baz.txt", null);
        await fileService.AddFile(_directoryPath, "foo2.txt", null);

        var options = new SearchOptionsDto("foo", 0, 100);

        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/foo.txt", filePaths);
        Assert.Contains($"{_directoryPath}/foo2.txt", filePaths);
    }

    [Theory, AutoData]
    public async Task SearchFiles_FiltersFilesByTags(
        Sut<SearchService> sut,
        IFileService fileService,
        ITagService tagService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "foo.txt", null);
        await fileService.AddFile(_directoryPath, "bar.txt", null);
        await fileService.AddFile(_directoryPath, "baz.txt", null);

        await tagService.AddFileTag(_directoryPath, "foo.txt", "tag1", null);
        await tagService.AddFileTag(_directoryPath, "bar.txt", "tag2", null);
        await tagService.AddFileTag(_directoryPath, "baz.txt", "tag2", null);
        await tagService.AddFileTag(_directoryPath, "baz.txt", "tag3", null);

        var options = new SearchOptionsDto("", 0, 100, Tags: ["tag2"]);

        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/bar.txt", filePaths);
        Assert.Contains($"{_directoryPath}/baz.txt", filePaths);
    }

    [Theory, AutoData]
    public async Task SearchFiles_NoSearchTermOrFilters_ReturnsAllFiles(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "foo.txt", null);
        await fileService.AddFile(_directoryPath, "bar.md", null);
        await fileService.AddFile(_directoryPath, "baz.pdf", null);

        var options = new SearchOptionsDto("", 0, 100);
        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(3, filePaths.Count);
        Assert.Contains($"{_directoryPath}/foo.txt", filePaths);
        Assert.Contains($"{_directoryPath}/bar.md", filePaths);
        Assert.Contains($"{_directoryPath}/baz.pdf", filePaths);
    }

    [Theory, AutoData]
    public async Task SearchFiles_FiltersFilesByType(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "one.md", null);
        await fileService.AddFile(_directoryPath, "two.txt", null);
        await fileService.AddFile(_directoryPath, "three.pdf", null);

        var options = new SearchOptionsDto("", 0, 100, FileTypes: ["txt", ".pdf"]);
        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Contains($"{_directoryPath}/two.txt", filePaths);
        Assert.Contains($"{_directoryPath}/three.pdf", filePaths);
        Assert.Equal(2, filePaths.Count);
    }

    [Theory, AutoData]
    public async Task SearchFiles_FiltersFilesByModifiedDateRange(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "date1.txt", null);
        await fileService.AddFile(_directoryPath, "date2.txt", null);
        await fileService.AddFile(_directoryPath, "date3.txt", null);

        // Set file modified dates using direct DbContext modification (simulate file modified dates)
        var dbDirectory = await sut.Context.Directories
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
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // This should return all files between "2025-01-15" and "2025-02-15"
        var options = new SearchOptionsDto("", 0, 100,
            ModifiedFrom: DateTime.Parse("2025-01-15"), ModifiedTo: DateTime.Parse("2025-02-15"));

        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Single(filePaths);
        Assert.Contains($"{_directoryPath}/date2.txt", filePaths);


        // This should return all files after ModifiedFrom date
        options = options with { ModifiedTo = null };

        files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_directoryPath}/date2.txt", filePaths);
        Assert.Contains($"{_directoryPath}/date3.txt", filePaths);
    }


    [Theory, AutoData]
    public async Task SearchFiles_UnmatchedCriteria_ReturnsEmpty(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "abc.txt", null);
        await fileService.AddFile(_directoryPath, "def.md", null);

        var options = new SearchOptionsDto("xyz", 0, 100, FileTypes: ["pdf"], Tags: ["notag"]);
        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(files);
    }

    [Theory, AutoData]
    public async Task SearchFiles_ComplexFiltering_OnlyCorrectFileReturned(
        Sut<SearchService> sut,
        IFileService fileService,
        ITagService tagService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "special1.md", null);
        await fileService.AddFile(_directoryPath, "special2.md", null);
        await tagService.AddFileTag(_directoryPath, "special1.md", "projectA", null);

        var dbFile = sut.Context.Files.First(f => f.Name == "special1.md");
        dbFile.Modified = DateTime.UtcNow.AddDays(-5);
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new SearchOptionsDto(
            SearchTerm: "special1",
            Page: 0,
            PageSize: 100,
            Tags: ["projectA"],
            FileTypes: ["md"],
            ModifiedFrom: DateTime.UtcNow.AddDays(-10),
            ModifiedTo: DateTime.UtcNow.AddDays(-1)
        );
        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Single(filePaths);
        Assert.Contains($"{_directoryPath}/special1.md", filePaths);
    }

    [Theory, AutoData]
    public async Task SearchFiles_FileName_MatchingIsCaseInsensitive(
        Sut<SearchService> sut,
        IFileService fileService)
    {
        await CreateBaseDirectory(sut);

        await fileService.AddFile(_directoryPath, "FOO.TXT", null);
        var options = new SearchOptionsDto("foo", 0, 100);

        var files = await sut.Value.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(files);
        Assert.EndsWith("FOO.TXT", files[0].Path);
    }
}