using Api.Exceptions;
using Api.Services.Files;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.ServiceTests.Files;

public sealed class FileShareServiceTests
{
    private readonly FileShareService _service;
    private readonly RefNotesContext _context;
    private readonly FakerResolver _fakerResolver;

    public FileShareServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<FileShareService>()
            .WithDb(dbFixture)
            .WithFakers()
            .CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<FileShareService>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
    }

    [Fact]
    public async Task GenerateShareHash_GeneratesHash()
    {
        var file = _fakerResolver.Get<EncryptedFile>().Generate();

        var shareHash = await _service.GenerateShareHash(file.Id);

        Assert.NotNull(shareHash);
        Assert.NotEmpty(shareHash);
        var hashInDb = await _context.SharedFileHashes.FirstOrDefaultAsync(sf => sf.Hash == shareHash,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(hashInDb);
        Assert.False(hashInDb.IsDeleted);
    }

    [Fact]
    public async Task GenerateSharedFileFromHash_GeneratesSharedFile_WhenHashIsValid()
    {
        var sharedFileHash = _fakerResolver.Get<SharedFileHash>().Generate();
        var directory = _fakerResolver.Get<EncryptedDirectory>().Generate();

        var sharedFile = await _service.GenerateSharedFileFromHash(sharedFileHash.Hash, directory.Id);

        Assert.True(sharedFileHash.IsDeleted);
        Assert.NotNull(sharedFile);
        Assert.Equal(directory.Id, sharedFile.SharedToDirectoryId);
    }

    [Fact]
    public async Task GenerateSharedFileFromHash_Throws_WhenHashIsInvalid()
    {
        var sharedFileHash = _fakerResolver.Get<SharedFileHash>().AsDeleted().Generate();
        var directory = _fakerResolver.Get<EncryptedFile>().Generate();

        await Assert.ThrowsAsync<SharedFileHashNotFound>(async ()
            => await _service.GenerateSharedFileFromHash(sharedFileHash.Hash, directory.Id));
    }

    [Fact]
    public async Task GetOwnerFromHash_ReturnsOwner_WhenHashIsValid()
    {
        var sharedFileHash = _fakerResolver.Get<SharedFileHash>().Generate();
        var owner = sharedFileHash.EncryptedFile.EncryptedDirectory!.Owner!;

        var ownerFromHash = await _service.GetOwnerFromHash(sharedFileHash.Hash);

        Assert.Equal(owner, ownerFromHash);
    }
}
