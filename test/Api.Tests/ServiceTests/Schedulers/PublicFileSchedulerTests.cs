using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Services.Schedulers;
using Api.Tests.Data;
using NSubstitute;
using Quartz;

namespace Api.Tests.ServiceTests.Schedulers;

[SuppressMessage("Usage", "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileSchedulerTests
{
    [Theory, AutoData]
    public async Task ScheduleImageRefreshForPublicFile_SchedulesImageRefresh(
        Sut<PublicFileScheduler> sut,
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
        Sut<PublicFileScheduler> sut,
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
}
