using MigrationService;
using Server;

var builder = Host.CreateApplicationBuilder(args);

builder.AddDatabase();
builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

var host = builder.Build();
host.Run();
