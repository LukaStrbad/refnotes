using System.Diagnostics.CodeAnalysis;
using Api.Services;
using Api.Services.Schedulers;
using Api.Tests.Data;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;

namespace Api.Tests.ServiceTests.Schedulers;

[SuppressMessage("Usage", "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken")]
public sealed class PublicFileSchedulerTests
{
    private readonly PublicFileScheduler _service;
    private readonly ISchedulerFactory _schedulerFactory;
    
public PublicFileSchedulerTests(TestDatabaseFixture dbFixture)
    {
        var serviceProvider = new ServiceFixture<PublicFileScheduler>().WithDb(dbFixture).CreateServiceProvider();
        
        _service = serviceProvider.GetRequiredService<PublicFileScheduler>();
        _schedulerFactory = serviceProvider.GetRequiredService<ISchedulerFactory>();
    }
    
    [Fact]
    public async Task ScheduleImageRefreshForPublicFile_SchedulesImageRefresh()
    {
        const int publicFileId = 1234;
        var scheduler = Substitute.For<IScheduler>();
        _schedulerFactory.GetScheduler().Returns(scheduler);
        scheduler.CheckExists(Arg.Any<JobKey>()).Returns(false);

        await _service.ScheduleImageRefreshForPublicFile(publicFileId);

        var nowWithOffset = DateTime.UtcNow.AddSeconds(1);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(job => job.JobDataMap.GetIntValue("publicFileId") == publicFileId),
            Arg.Is<ITrigger>(trigger => trigger.StartTimeUtc <= nowWithOffset)
        );
    }

    [Fact]
    public async Task ScheduleImageRefreshForPublicFile_WhenJobExists_DeletesExistingJobAndSchedulesNew()
    {
        const int publicFileId = 1234;
        var scheduler = Substitute.For<IScheduler>();
        _schedulerFactory.GetScheduler().Returns(scheduler);
        scheduler.CheckExists(Arg.Any<JobKey>()).Returns(true);

        await _service.ScheduleImageRefreshForPublicFile(publicFileId);

        await scheduler.Received(1).Interrupt(Arg.Any<JobKey>());
        await scheduler.Received(1).DeleteJob(Arg.Any<JobKey>());
        await scheduler.Received(1).ScheduleJob(Arg.Any<IJobDetail>(), Arg.Any<ITrigger>());
    }
}
