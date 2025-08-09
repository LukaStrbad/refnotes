using Api.Model;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data.Model;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class SearchServiceTests : BaseTests
{
    private readonly SearchService _service;
    private readonly FakerResolver _fakerResolver;

    private readonly EncryptedDirectory _defaultDir;

    public SearchServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<SearchService>().WithDb(dbFixture).WithFakeEncryption().WithFakers()
            .WithRedis().CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<SearchService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        
        var defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultDir = _fakerResolver.Get<EncryptedDirectory>().ForUser(defaultUser).Generate();
        serviceProvider.GetRequiredService<IUserService>().GetCurrentUser().Returns(defaultUser);
    }

    [Fact]
    public async Task SearchFiles_SearchesFilesByName()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).Generate(3);
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("test.txt").Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("test2.txt").Generate();
        var options = new SearchOptionsDto("test", 0, 100);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_defaultDir.Path}/test.txt", filePaths);
        Assert.Contains($"{_defaultDir.Path}/test2.txt", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByTags()
    {
        // Arrange
        var existingFiles = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).Generate(3);
        var (file1, file2, file3) = (existingFiles[0], existingFiles[1], existingFiles[2]);
        _fakerResolver.Get<FileTag>().WithName("tag1").ForFiles(file1).Generate();
        _fakerResolver.Get<FileTag>().WithName("tag2").ForFiles(file2, file3).Generate();
        _fakerResolver.Get<FileTag>().WithName("tag3").ForFiles(file3).Generate();
        var options = new SearchOptionsDto("", 0, 100, Tags: ["tag2"]);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(file => file.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_defaultDir.Path}/{file2.Name}", filePaths);
        Assert.Contains($"{_defaultDir.Path}/{file3.Name}", filePaths);
    }

    [Fact]
    public async Task SearchFiles_NoSearchTermOrFilters_ReturnsAllFiles()
    {
        // Arrange
        var generatedFiles = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).Generate(10);
        var generatedFilePaths = generatedFiles.Select(f => $"{_defaultDir.Path}/{f.Name}").ToList();
        var options = new SearchOptionsDto("", 0, 100);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(generatedFiles.Count, filePaths.Count);
        Assert.Equal(generatedFilePaths, filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByType()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("one.md").Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("two.txt").Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("three.pdf").Generate();
        var options = new SearchOptionsDto("", 0, 100, FileTypes: ["txt", ".pdf"]);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(2, filePaths.Count);
        Assert.Contains($"{_defaultDir.Path}/two.txt", filePaths);
        Assert.Contains($"{_defaultDir.Path}/three.pdf", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesByModifiedDateRange()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 1, 1)).Generate(2);
        var februaryFiles = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 2, 1))
            .Generate(3);
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 3, 1)).Generate(5);

        var options = new SearchOptionsDto("", 0, 100,
            ModifiedFrom: new DateTime(2025, 1, 15), ModifiedTo: new DateTime(2025, 2, 15));

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(3, filePaths.Count);
        Assert.Equal(februaryFiles.Select(f => $"{_defaultDir.Path}/{f.Name}").ToList(), filePaths);
    }

    [Fact]
    public async Task SearchFiles_FiltersFilesMyModifiedFrom()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 1, 1)).Generate(2);
        var februaryFiles = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 2, 1))
            .Generate(3);
        var marchFiles = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithModifiedDate(new DateTime(2025, 3, 1))
            .Generate(5);
        List<EncryptedFile> combinedFiles = [..februaryFiles, ..marchFiles];

        var options = new SearchOptionsDto("", 0, 100,
            ModifiedFrom: new DateTime(2025, 1, 15), ModifiedTo: null);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Equal(8, filePaths.Count);
        Assert.Equal(combinedFiles.Select(f => $"{_defaultDir.Path}/{f.Name}").ToList(), filePaths);
    }


    [Fact]
    public async Task SearchFiles_UnmatchedCriteria_ReturnsEmpty()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).Generate(5);
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("test.txt").Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("test2.md").Generate();

        // Act
        var options = new SearchOptionsDto("xyz", 0, 100, FileTypes: ["pdf"], Tags: ["notag"]);
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task SearchFiles_ComplexFiltering_OnlyCorrectFileReturned()
    {
        // Arrange
        var file1 = _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("special1.md")
            .WithModifiedDate(DateTime.UtcNow.AddDays(-5)).Generate();
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("special2.md").Generate();
        _fakerResolver.Get<FileTag>().WithName("projectA").ForFiles(file1).Generate();
        var options = new SearchOptionsDto(
            SearchTerm: "special1",
            Page: 0,
            PageSize: 100,
            Tags: ["projectA"],
            FileTypes: ["md"],
            ModifiedFrom: DateTime.UtcNow.AddDays(-10),
            ModifiedTo: DateTime.UtcNow.AddDays(-1)
        );

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var filePaths = files.Select(f => f.Path).ToList();
        Assert.Single(filePaths);
        Assert.Contains($"{_defaultDir.Path}/special1.md", filePaths);
    }

    [Fact]
    public async Task SearchFiles_FileName_MatchingIsCaseInsensitive()
    {
        // Arrange
        _fakerResolver.Get<EncryptedFile>().ForDir(_defaultDir).WithName("FOO.TXT").Generate();
        var options = new SearchOptionsDto("foo", 0, 100);

        // Act
        var files = await _service.SearchFiles(options)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(files);
        Assert.EndsWith("FOO.TXT", files[0].Path);
    }
}
