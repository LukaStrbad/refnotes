using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace ServiceDefaults;

public static class Scheduler
{
    public static void AddSchedulerServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddKeyedMySqlDataSource("scheduler");
        builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));
        builder.Services.AddQuartz(configure: quartzBuilder =>
        {
            quartzBuilder.UsePersistentStore(x =>
            {
                x.UseClustering();
                x.UseMySql(options =>
                {
                    options.ConnectionStringName = "scheduler";
                });
                x.UseSystemTextJsonSerializer();
            });
        });
    }

    public static void AddSchedulerHost(this IHostApplicationBuilder builder)
    {
        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }
}
