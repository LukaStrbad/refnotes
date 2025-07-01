using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Tests.Data;
using Api.Tests.Data.Faker;
using Data.Model;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Quartz;

namespace Api.Tests.ServiceTests;

[SuppressMessage("Usage",
    "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileImageServiceTests : BaseTests
{
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
        IFileService fileService, 
        IFileStorageService fileStorageService,
        DatabaseFaker<EncryptedFile> encryptedFileFaker,
        DatabaseFaker<PublicFile> publicFileFaker)
    {
        const string imageName = "image.png";
        var encryptedFile = encryptedFileFaker.Generate();
        var publicFile = publicFileFaker.ForFile(encryptedFile).Generate();
        var image = encryptedFileFaker.WithName(imageName).Generate();
        
        fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(StreamFromString($"![alt]({imageName})"));
        fileService.GetFilePathAsync(encryptedFile.Id).Returns($"/{encryptedFile.Name}");
        fileService.GetEncryptedFileAsync($"/{imageName}", null).Returns(image);

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
        IFileService fileService,
        IFileStorageService fileStorageService,
        DatabaseFaker<EncryptedFile> encryptedFileFaker,
        DatabaseFaker<PublicFile> publicFileFaker)
    {
        var encryptedFile = encryptedFileFaker.Generate();
        var image1 = encryptedFileFaker.AsImage().Generate();
        var image2 = encryptedFileFaker.AsImage().Generate();
        var publicFile = publicFileFaker.ForFile(encryptedFile).Generate();

        fileStorageService.GetFile(encryptedFile.FilesystemName).Returns(
            StreamFromString($"![alt]({image1.Name})"),
            StreamFromString($"![alt]({image1.Name})\n![alt2]({image2.Name})")
        );
        fileService.GetFilePathAsync(encryptedFile.Id).Returns($"/{encryptedFile.Name}");
        fileService.GetEncryptedFileAsync($"/{image1.Name}", null).Returns(image1);
        fileService.GetEncryptedFileAsync($"/{image2.Name}", null).Returns(image2);

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
