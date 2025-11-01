using MigrationService;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSchedulerServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddSingleton<ISchedulerMigrations, PostgresSchedulerMigrations>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

var host = builder.Build();
host.Run();
