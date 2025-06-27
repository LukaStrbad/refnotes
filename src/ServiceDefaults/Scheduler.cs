using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Quartz;
using Quartz.Logging;

namespace ServiceDefaults;

public static class Scheduler
{
    private const int DefaultThreadPoolSize = 10;

    private static int GetThreadPoolSize(IServiceProvider services)
    {
        if (services.GetService<IConfiguration>() is not { } configuration)
            return DefaultThreadPoolSize;

        return configuration.GetValue("Scheduler:ThreadPoolSize", DefaultThreadPoolSize) > 0
            ? configuration.GetValue<int>("Scheduler:ThreadPoolSize")
            : DefaultThreadPoolSize;
    }

    public static async Task<IScheduler> GetScheduler(IServiceProvider services)
    {
        var threadPoolSize = GetThreadPoolSize(services);
        var dataSource = services.GetRequiredService<MySqlDataSource>();

        var scheduler = await SchedulerBuilder.Create()
            .UseDefaultThreadPool(x => x.MaxConcurrency = threadPoolSize)
            .UsePersistentStore(x =>
            {
                x.UseProperties = true;
                x.UseClustering();
                x.UseMySql(dataSource.ConnectionString);
                x.UseSystemTextJsonSerializer();
            })
            .BuildScheduler();
        
        LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
        
        return scheduler;
    }

    public static void AddSchedulerServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddKeyedMySqlDataSource("scheduler");
        builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));
        builder.Services.AddQuartz(configure: quartzBuilder =>
        {
            quartzBuilder.UseDefaultThreadPool(x => x.MaxConcurrency = GetThreadPoolSize(builder.Services.BuildServiceProvider()));
            quartzBuilder.UsePersistentStore(x =>
            {
                x.UseProperties = true;
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
    
    private class ConsoleLogProvider : ILogProvider
    {
        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Info && func != null)
                {
                    Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                }
                return true;
            };
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }
    }
}
