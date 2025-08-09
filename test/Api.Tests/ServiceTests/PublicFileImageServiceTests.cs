using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Tests.Extensions.Faker;
using Api.Tests.Fixtures;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

[SuppressMessage("Usage",
    "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileImageServiceTests : BaseTests
{
    private readonly PublicFileImageService _service;
    private readonly IFileService _fileService;
    private readonly IFileStorageService _fileStorageService;
    private readonly FakerResolver _fakerResolver;
    private readonly RefNotesContext _context;

    private readonly User _defaultUser;
    private readonly UserGroup _defaultGroup;

    public PublicFileImageServiceTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<PublicFileImageService>().WithDb(dbFixture).WithFakeEncryption()
            .WithFakers().CreateServiceProvider();

        _service = serviceProvider.GetRequiredService<PublicFileImageService>();
        _fileService = serviceProvider.GetRequiredService<IFileService>();
        _fileStorageService = serviceProvider.GetRequiredService<IFileStorageService>();
        _fakerResolver = serviceProvider.GetRequiredService<FakerResolver>();
        _context = serviceProvider.GetRequiredService<RefNotesContext>();

        _defaultUser = _fakerResolver.Get<User>().Generate();
        _defaultGroup = _fakerResolver.Get<UserGroup>().Generate();
    }

    private DirOwner GetDirOwner(User user, UserGroup? group)
    {
        return group is not null ? new DirOwner(group) : new DirOwner(user);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UpdateImagesForPublicFile_UpdatesImages(bool withGroup)
    {
        const string imageName = "image.png";
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUserOrGroup(_defaultUser, group).Generate();
        var encryptedFile = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(encryptedFile).Generate();
        var image = _fakerResolver.Get<EncryptedFile>().WithName(imageName).Generate();

        var dirOwner = GetDirOwner(_defaultUser, group);
        _fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        _fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(StreamFromString($"![alt]({imageName})"));
        _fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        _fileService.GetEncryptedFileForOwnerAsync($"/{imageName}", dirOwner).Returns(image);

        await _service.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await _context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(publicFileImages);
        Assert.Equal(image.Id, publicFileImages[0].EncryptedFileId);
    }

    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UpdateImagesForPublicFile_AddsNewImages_WhenContentUpdates(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var encryptedFile = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var image1 = _fakerResolver.Get<EncryptedFile>().WithName("image1.png").Generate();
        var image2 = _fakerResolver.Get<EncryptedFile>().WithName("image2.png").Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(encryptedFile).Generate();

        var dirOwner = GetDirOwner(_defaultUser, group);
        _fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        _fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(
            StreamFromString($"![alt]({image1.Name})"),
            StreamFromString($"![alt]({image1.Name})\n![alt2]({image2.Name})")
        );
        _fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        _fileService.GetEncryptedFileForOwnerAsync($"/{image1.Name}", dirOwner).Returns(image1);
        _fileService.GetEncryptedFileForOwnerAsync($"/{image2.Name}", dirOwner).Returns(image2);

        // Update with initial content and then update with new content
        await _service.UpdateImagesForPublicFile(publicFile.Id);
        await _service.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await _context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, publicFileImages.Count);
        Assert.Equal(image1.Id, publicFileImages[0].EncryptedFileId);
        Assert.Equal(image2.Id, publicFileImages[1].EncryptedFileId);
    }


    [Theory]
    [InlineData(false), InlineData(true)]
    public async Task UpdateImagesForPublicFile_UpdatesImages_WithDuplicates(bool withGroup)
    {
        var group = withGroup ? _defaultGroup : null;
        var dir = _fakerResolver.Get<EncryptedDirectory>().ForUser(_defaultUser).Generate();
        var encryptedFile = _fakerResolver.Get<EncryptedFile>().ForDir(dir).Generate();
        var image1 = _fakerResolver.Get<EncryptedFile>().WithName("image1.png").Generate();
        var image2 = _fakerResolver.Get<EncryptedFile>().WithName("image2.png").Generate();
        var publicFile = _fakerResolver.Get<PublicFile>().ForFile(encryptedFile).Generate();

        var dirOwner = GetDirOwner(_defaultUser, group);
        _fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        _fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(
            StreamFromString($"![alt]({image1.Name})\n![alt2]({image2.Name})\n![alt3]({image2.Name})")
        );
        _fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        _fileService.GetEncryptedFileForOwnerAsync(Arg.Any<string>(), dirOwner).Returns(image1, image2);

        // Update with initial content and then update with new content
        await _service.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await _context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, publicFileImages.Count);
        Assert.Equal(image1.Id, publicFileImages[0].EncryptedFileId);
        Assert.Equal(image2.Id, publicFileImages[1].EncryptedFileId);
    }

    [Fact]
    public async Task RemoveImagesForPublicFile_RemovesImages()
    {
        var publicFile = _fakerResolver.Get<PublicFile>().Generate();
        _fakerResolver.Get<PublicFileImage>().RuleFor(i => i.PublicFileId, _ => publicFile.Id).Generate(3);

        await _service.RemoveImagesForPublicFile(publicFile.Id);

        var publicFileImagesList =
            await _context.PublicFileImages.Where(pfi => pfi.PublicFileId == publicFile.Id).ToListAsync();
        Assert.Empty(publicFileImagesList);
    }
}
