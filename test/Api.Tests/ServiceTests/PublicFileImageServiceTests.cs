using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Data.Faker;
using Api.Tests.Data.Faker.Definition;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Quartz;

namespace Api.Tests.ServiceTests;

[SuppressMessage("Usage",
    "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileImageServiceTests : BaseTests
{
    private DirOwner GetDirOwner(User user, UserGroup? group)
    {
        return group is not null ? new DirOwner(group) : new DirOwner(user);
    }

    [Theory, AutoData]
    public async Task UpdateImagesForPublicFile_UpdatesImages(
        Sut<PublicFileImageService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService,
        IFileStorageService fileStorageService,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker,
        PublicFileFakerImplementation publicFileFaker)
    {
        const string imageName = "image.png";
        var dir = dirFaker.CreateFaker().ForUserOrGroup(sut.DefaultUser, group).Generate();
        var encryptedFile = fileFaker.CreateFaker().ForDir(dir).Generate();
        var publicFile = publicFileFaker.CreateFaker().ForFile(encryptedFile).Generate();
        var image = fileFaker.CreateFaker().WithName(imageName).Generate();

        var dirOwner = GetDirOwner(sut.DefaultUser, group);
        fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(StreamFromString($"![alt]({imageName})"));
        fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        fileService.GetEncryptedFileForOwnerAsync($"/{imageName}", dirOwner).Returns(image);

        await sut.Value.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await sut.Context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(publicFileImages);
        Assert.Equal(image.Id, publicFileImages[0].EncryptedFileId);
    }

    [Theory, AutoData]
    public async Task UpdateImagesForPublicFile_AddsNewImages_WhenContentUpdates(
        Sut<PublicFileImageService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService,
        IFileStorageService fileStorageService,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker,
        PublicFileFakerImplementation publicFileFaker)
    {
        var dir = dirFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();
        var encryptedFile = fileFaker.CreateFaker().ForDir(dir).Generate();
        var image1 = fileFaker.CreateFaker().WithName("image1.png").Generate();
        var image2 = fileFaker.CreateFaker().WithName("image2.png").Generate();
        var publicFile = publicFileFaker.CreateFaker().ForFile(encryptedFile).Generate();

        var dirOwner = GetDirOwner(sut.DefaultUser, group);
        fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(
            StreamFromString($"![alt]({image1.Name})"),
            StreamFromString($"![alt]({image1.Name})\n![alt2]({image2.Name})")
        );
        fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        fileService.GetEncryptedFileForOwnerAsync($"/{image1.Name}", dirOwner).Returns(image1);
        fileService.GetEncryptedFileForOwnerAsync($"/{image2.Name}", dirOwner).Returns(image2);

        // Update with initial content and then update with new content
        await sut.Value.UpdateImagesForPublicFile(publicFile.Id);
        await sut.Value.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await sut.Context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, publicFileImages.Count);
        Assert.Equal(image1.Id, publicFileImages[0].EncryptedFileId);
        Assert.Equal(image2.Id, publicFileImages[1].EncryptedFileId);
    }


    [Theory, AutoData]
    public async Task UpdateImagesForPublicFile_UpdatesImages_WithDuplicates(
        Sut<PublicFileImageService> sut,
        [FixtureGroup(AddNull = true)] UserGroup? group,
        IFileService fileService,
        IFileStorageService fileStorageService,
        EncryptedDirectoryFakerImplementation dirFaker,
        EncryptedFileFakerImplementation fileFaker,
        DatabaseFaker<PublicFile> publicFileFaker)
    {
        var dir = dirFaker.CreateFaker().ForUser(sut.DefaultUser).Generate();
        var encryptedFile = fileFaker.CreateFaker().ForDir(dir).Generate();
        var image1 = fileFaker.CreateFaker().WithName("image1.png").Generate();
        var image2 = fileFaker.CreateFaker().WithName("image2.png").Generate();
        var publicFile = publicFileFaker.ForFile(encryptedFile).Generate();

        var dirOwner = GetDirOwner(sut.DefaultUser, group);
        fileService.GetDirOwnerAsync(encryptedFile).Returns(dirOwner);
        fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(
            StreamFromString($"![alt]({image1.Name})\n![alt2]({image2.Name})\n![alt3]({image2.Name})")
        );
        fileService.GetFilePathAsync(encryptedFile).Returns($"/{encryptedFile.Name}");
        fileService.GetEncryptedFileForOwnerAsync(Arg.Any<string>(), dirOwner).Returns(image1, image2);

        // Update with initial content and then update with new content
        await sut.Value.UpdateImagesForPublicFile(publicFile.Id);

        Assert.NotNull(publicFile);
        var publicFileImages = await sut.Context.PublicFileImages
            .Where(pfi => pfi.PublicFileId == publicFile.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, publicFileImages.Count);
        Assert.Equal(image1.Id, publicFileImages[0].EncryptedFileId);
        Assert.Equal(image2.Id, publicFileImages[1].EncryptedFileId);
    }

    [Theory, AutoData]
    public async Task RemoveImagesForPublicFile_RemovesImages(
        Sut<PublicFileImageService> sut,
        DatabaseFaker<PublicFile> publicFileFaker,
        DatabaseFaker<PublicFileImage> publicFileImageFaker)
    {
        var publicFile = publicFileFaker.Generate();
        publicFileImageFaker.RuleFor(i => i.PublicFileId, _ => publicFile.Id).Generate(3);

        await sut.Value.RemoveImagesForPublicFile(publicFile.Id);

        var publicFileImagesList =
            await sut.Context.PublicFileImages.Where(pfi => pfi.PublicFileId == publicFile.Id).ToListAsync();
        Assert.Empty(publicFileImagesList);
    }
}
