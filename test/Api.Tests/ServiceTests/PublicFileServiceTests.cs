using Api.Services;
using Api.Tests.Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests.ServiceTests;

public class PublicFileServiceTests : BaseTests
{
    private static async Task<EncryptedFile> CreateFile(Sut<PublicFileService> sut)
    {
        var rndPath = $"/test_dir_{RandomString(32)}";
        var directory = new EncryptedDirectory(rndPath, sut.DefaultUser);
        await sut.Context.Directories.AddAsync(directory);

        var fileName = $"{RandomString(32)}.txt";
        var newFile = new EncryptedFile("test123", fileName)
        {
            EncryptedDirectory = directory
        };
        await sut.Context.Files.AddAsync(newFile);

        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return newFile;
    }

    [Theory, AutoData]
    public async Task GetUrlHash_GetsUrlHash(Sut<PublicFileService> sut)
    {
        var expectedUrlHash = RandomString(64);
        var file = await CreateFile(sut);
        await sut.Context.PublicFiles.AddAsync(new PublicFile(expectedUrlHash, file.Id),
            TestContext.Current.CancellationToken);
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var urlHash = await sut.Value.GetUrlHashAsync(file.Id);

        Assert.Equal(expectedUrlHash, urlHash);
    }

    [Theory, AutoData]
    public async Task GetUrlHash_ReturnsNull_WhenFileNotFound(Sut<PublicFileService> sut)
    {
        var urlHash = await sut.Value.GetUrlHashAsync(0);

        Assert.Null(urlHash);
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_CreatesPublicFile(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var urlHash = await sut.Value.CreatePublicFileAsync(file.Id);

        var publicFile = await sut.Context.PublicFiles.FirstOrDefaultAsync(pf => pf.EncryptedFileId == file.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(urlHash);
        Assert.NotNull(publicFile);
        Assert.Equal(urlHash, publicFile.UrlHash);
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_ThrowsExceptionIfFileNotFound(Sut<PublicFileService> sut)
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => sut.Value.CreatePublicFileAsync(0));
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_ReturnsExistingUrlHash(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var urlHash = await sut.Value.CreatePublicFileAsync(file.Id);
        await sut.Value.DeactivatePublicFileAsync(file.Id);
        var urlHash2 = await sut.Value.CreatePublicFileAsync(file.Id);

        var publicFiles = await sut.Context.PublicFiles.Where(pf => pf.EncryptedFileId == file.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(publicFiles);
        Assert.Equal(PublicFileState.Active, publicFiles.First().State);
        Assert.Equal(urlHash, publicFiles.First().UrlHash);
        Assert.Equal(urlHash, urlHash2);
    }

    [Theory, AutoData]
    public async Task DeactivatePublicFileAsync_DeactivatesPublicFile(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        await sut.Value.CreatePublicFileAsync(file.Id);
        var deleteResult = await sut.Value.DeactivatePublicFileAsync(file.Id);

        var publicFile = await sut.Context.PublicFiles.FirstOrDefaultAsync(pf => pf.EncryptedFileId == file.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(deleteResult);
        Assert.NotNull(publicFile);
        Assert.Equal(PublicFileState.Inactive, publicFile.State);
    }

    [Theory, AutoData]
    public async Task DeactivatePublicFileAsync_ReturnsFalse_WhenFileNotFound(Sut<PublicFileService> sut)
    {
        var deleteResult = await sut.Value.DeactivatePublicFileAsync(0);

        Assert.False(deleteResult);
    }

    [Theory, AutoData]
    public async Task IsPublicFileActive_ReturnsTrue_WhenFileActive(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var fileHash = await sut.Value.CreatePublicFileAsync(file.Id);

        var isActive = await sut.Value.IsPublicFileActive(fileHash);
        Assert.True(isActive);
    }

    [Theory, AutoData]
    public async Task IsPublicFileActive_ReturnsFalse_WhenFileInactive(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var fileHash = await sut.Value.CreatePublicFileAsync(file.Id);
        await sut.Value.DeactivatePublicFileAsync(file.Id);

        var isActive = await sut.Value.IsPublicFileActive(fileHash);
        Assert.False(isActive);
    }
}