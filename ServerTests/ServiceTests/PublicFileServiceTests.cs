using Microsoft.EntityFrameworkCore;
using Server.Db.Model;
using Server.Services;
using ServerTests.Data;

namespace ServerTests.ServiceTests;

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

        var urlHash = await sut.Value.GetUrlHash(file.Id);

        Assert.Equal(expectedUrlHash, urlHash);
    }

    [Theory, AutoData]
    public async Task GetUrlHash_ReturnsNull_WhenFileNotFound(Sut<PublicFileService> sut)
    {
        var urlHash = await sut.Value.GetUrlHash(0);

        Assert.Null(urlHash);
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_CreatesPublicFile(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var urlHash = await sut.Value.CreatePublicFile(file.Id);

        var publicFile = await sut.Context.PublicFiles.FirstOrDefaultAsync(pf => pf.EncryptedFileId == file.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(urlHash);
        Assert.NotNull(publicFile);
        Assert.Equal(urlHash, publicFile.UrlHash);
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_ThrowsExceptionIfFileNotFound(Sut<PublicFileService> sut)
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => sut.Value.CreatePublicFile(0));
    }

    [Theory, AutoData]
    public async Task CreatePublicFile_ReturnsExistingUrlHash(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        var urlHash = await sut.Value.CreatePublicFile(file.Id);
        var urlHash2 = await sut.Value.CreatePublicFile(file.Id);

        var publicFiles = await sut.Context.PublicFiles.Where(pf => pf.EncryptedFileId == file.Id)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        
        Assert.Single(publicFiles);
        Assert.Equal(urlHash, publicFiles.First().UrlHash);
        Assert.Equal(urlHash, urlHash2);
    }
    
    [Theory, AutoData]
    public async Task DeletePublicFile_DeletesPublicFile(Sut<PublicFileService> sut)
    {
        var file = await CreateFile(sut);

        await sut.Value.CreatePublicFile(file.Id);
        var deleteResult = await sut.Value.DeletePublicFile(file.Id);
        
        var publicFile = await sut.Context.PublicFiles.FirstOrDefaultAsync(pf => pf.EncryptedFileId == file.Id,
            cancellationToken: TestContext.Current.CancellationToken);
        
        Assert.True(deleteResult);
        Assert.Null(publicFile);
    }
    
    [Theory, AutoData]
    public async Task DeletePublicFile_ReturnsFalse_WhenFileNotFound(Sut<PublicFileService> sut)
    {
        var deleteResult = await sut.Value.DeletePublicFile(0);
        
        Assert.False(deleteResult);
    }
}