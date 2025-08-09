using Api.Services;
using Api.Services.Schedulers;
using Api.Tests.Data.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class PublicFileServiceTests : BaseTests
{
    private readonly PublicFileService _service;
    private readonly IPublicFileScheduler _publicFileScheduler;
    private readonly RefNotesContext _context;
    private readonly FakerResolver _fakerResolver;

    public PublicFileServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<PublicFileService>().WithDb(dbFixture).WithFakeEncryption()
            .WithFakers().CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<PublicFileService>();
        _publicFileScheduler = serviceProvider.GetRequiredService<IPublicFileScheduler>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
    }

    [Fact]
    public async Task GetUrlHash_GetsUrlHash()
    {
        var file = _fakerResolver.Get<EncryptedFile>().Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(file).Generate();

        var urlHash = await _service.GetUrlHashAsync(file.Id);

        Assert.Equal(publicFile.UrlHash, urlHash);
    }

    [Fact]
    public async Task GetUrlHash_ReturnsNull_WhenFileNotFound()
    {
        var urlHash = await _service.GetUrlHashAsync(0);

        Assert.Null(urlHash);
    }

    [Fact]
    public async Task CreatePublicFile_CreatesPublicFile()
    {
        // Arrange
        var file = _fakerResolver.Get<EncryptedFile>().Generate();

        // Act
        var urlHash = await _service.CreatePublicFileAsync(file.Id);

        // Assert
        var publicFile = await _context.PublicFiles.FirstOrDefaultAsync(pf => pf.EncryptedFileId == file.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotEmpty(urlHash);
        Assert.NotNull(publicFile);
        Assert.Equal(urlHash, publicFile.UrlHash);
        await _publicFileScheduler.Received().ScheduleImageRefreshForPublicFile(publicFile.Id);
    }

    [Fact]
    public async Task CreatePublicFile_ThrowsExceptionIfFileNotFound()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.CreatePublicFileAsync(0));
    }

    [Fact]
    public async Task CreatePublicFile_ReturnsExistingUrlHash()
    {
        // Arrange
        var file = _fakerResolver.Get<EncryptedFile>().Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(file).Generate();

        // Act
        var urlHash = await _service.CreatePublicFileAsync(file.Id);

        // Assert
        Assert.Equal(publicFile.UrlHash, urlHash);
    }

    [Fact]
    public async Task DeactivatePublicFileAsync_DeactivatesPublicFile()
    {
        // Arrange
        var file = _fakerResolver.Get<EncryptedFile>().Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(file).Generate();
        
        // Act
        var deleteResult = await _service.DeactivatePublicFileAsync(file.Id);

        // Assert
        Assert.True(deleteResult);
        Assert.NotNull(publicFile);
        Assert.Equal(PublicFileState.Inactive, publicFile.State);
    }

    [Fact]
    public async Task DeactivatePublicFileAsync_ReturnsFalse_WhenFileNotFound()
    {
        var deleteResult = await _service.DeactivatePublicFileAsync(0);

        Assert.False(deleteResult);
    }

    [Fact]
    public async Task IsPublicFileActive_ReturnsTrue_WhenFileActive()
    {
        var file = _fakerResolver.Get<EncryptedFile>().Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(file).Generate();

        var isActive = await _service.IsPublicFileActive(publicFile.UrlHash);
        
        Assert.True(isActive);
    }

    [Fact]
    public async Task IsPublicFileActive_ReturnsFalse_WhenFileInactive()
    {
        var file = _fakerResolver.Get<EncryptedFile>().Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(file).Inactive().Generate();

        var isActive = await _service.IsPublicFileActive(publicFile.UrlHash);
        
        Assert.False(isActive);
    }

    [Fact]
    public async Task HasAccessToFileThroughHash_ReturnsTrue_WhenImageIsInPublicFile()
    {
        var publicFile = _fakerResolver.Get<PublicFile>().Generate();
        var publicImage = _fakerResolver.Get<PublicFileImage>().ForPublicFile(publicFile).Generate();

        var hasAccess = await _service.HasAccessToFileThroughHash(publicFile.UrlHash, publicImage.EncryptedFile!);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task HasAccessToFileThroughHash_ReturnsFalse_WhenImageIsNotInPublicFile()
    {
        var publicFile = _fakerResolver.Get<PublicFile>().Generate();
        var encryptedFile = _fakerResolver.Get<EncryptedFile>().Generate();

        var hasAccess = await _service.HasAccessToFileThroughHash(publicFile.UrlHash, encryptedFile);

        Assert.False(hasAccess);
    }
}
