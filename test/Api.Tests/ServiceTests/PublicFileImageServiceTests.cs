using System.Diagnostics.CodeAnalysis;
using System.Text;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Attributes;
using Api.Tests.Data.Faker;
using Api.Tests.Mocks;
using Api.Utils;
using Bogus;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;

namespace Api.Tests.ServiceTests;

[ConcreteType<IFileService, FileService>]
[ConcreteType<IBrowserService, BrowserService>]
[ConcreteType<IPublicFileService, PublicFileService>]
[ConcreteType<IFileStorageService, FileStorageService>]
[ConcreteType<IEncryptionService, FakeEncryptionService>]
[ConcreteType<IFileServiceUtils, FileServiceUtils>]
[SuppressMessage("Usage",
    "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileImageServiceTests : BaseTests
{
    private readonly string _fileName = $"{RandomString(32)}.md";

    private static async Task<EncryptedFile> CreateFile(Sut<PublicFileImageService> sut, string fileName,
        string? content = null)
    {
        // Assert root directory exists
        await sut.ServiceProvider.GetRequiredService<IBrowserService>()
            .AddDirectory("/", null);
        var filesystemName = await sut.ServiceProvider.GetRequiredService<IFileService>()
            .AddFile("/", fileName, null);

        if (content is not null)
        {
            var fileContentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await sut.ServiceProvider.GetRequiredService<IFileStorageService>()
                .SaveFileAsync(filesystemName, fileContentStream);
        }

        var encryptedFile = await sut.ServiceProvider.GetRequiredService<IFileService>()
            .GetEncryptedFileAsync($"/{fileName}", null);
        Assert.NotNull(encryptedFile);
        return encryptedFile;
    }

    [Theory, AutoData]
    public async Task ScheduleImageRefreshForPublicFile_SchedulesImageRefresh(
        Sut<PublicFileImageService> sut,
        ISchedulerFactory schedulerFactory)
    {
        const int publicFileId = 1234;
        var scheduler = Substitute.For<IScheduler>();
        schedulerFactory.GetScheduler().Returns(scheduler);
        scheduler.CheckExists(Arg.Any<JobKey>()).Returns(false);

        await sut.Value.ScheduleImageRefreshForPublicFile(publicFileId);

        var nowWithOffset = DateTime.UtcNow.AddSeconds(1);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(job => job.JobDataMap.GetIntValue("publicFileId") == publicFileId),
            Arg.Is<ITrigger>(trigger => trigger.StartTimeUtc <= nowWithOffset)
        );
    }

    [Theory, AutoData]
    public async Task ScheduleImageRefreshForPublicFile_WhenJobExists_DeletesExistingJobAndSchedulesNew(
        Sut<PublicFileImageService> sut,
        ISchedulerFactory schedulerFactory)
    {
        const int publicFileId = 1234;
        var scheduler = Substitute.For<IScheduler>();
        schedulerFactory.GetScheduler().Returns(scheduler);
        scheduler.CheckExists(Arg.Any<JobKey>()).Returns(true);

        await sut.Value.ScheduleImageRefreshForPublicFile(publicFileId);

        await scheduler.Received(1).Interrupt(Arg.Any<JobKey>());
        await scheduler.Received(1).DeleteJob(Arg.Any<JobKey>());
        await scheduler.Received(1).ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>());
    }

    [Theory, AutoData]
    public async Task UpdateImagesForPublicFile_UpdatesImages(
        Sut<PublicFileImageService> sut,
        IPublicFileService publicFileService)
    {
        const string imageName = "image.png";
        const string fileContent = $"![alt]({imageName})";

        var encryptedFile = await CreateFile(sut, _fileName, fileContent);
        var image = await CreateFile(sut, imageName);
        var hash = await publicFileService.CreatePublicFileAsync(encryptedFile.Id);

        await sut.Value.UpdateImagesForPublicFile(encryptedFile.Id);

        var publicFile = await publicFileService.GetPublicFileAsync(hash);
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
        IPublicFileService publicFileService,
        IFileStorageService fileStorageService)
    {
        const string imageName1 = "image.png";
        const string imageName2 = "image2.png";
        const string fileContent = $"![alt]({imageName1})";
        const string newFileContent = $"![alt]({imageName1})\n![alt2]({imageName2})";

        var encryptedFile = await CreateFile(sut, _fileName, fileContent);
        var image1 = await CreateFile(sut, imageName1);
        var image2 = await CreateFile(sut, imageName2);
        var hash = await publicFileService.CreatePublicFileAsync(encryptedFile.Id);

        await sut.Value.UpdateImagesForPublicFile(encryptedFile.Id);
        await fileStorageService.SaveFileAsync($"/{_fileName}",
            new MemoryStream(Encoding.UTF8.GetBytes(newFileContent)));
        await sut.Value.UpdateImagesForPublicFile(encryptedFile.Id);

        var publicFile = await publicFileService.GetPublicFileAsync(hash);
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
