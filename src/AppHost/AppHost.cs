using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var connectionString = builder.AddConnectionString("main");
var schedulerConnectionString = builder.AddConnectionString("scheduler");

var insightPort = builder.Configuration.GetValue<int?>("Ports:RedisInsight");
var cache = builder.AddRedis("cache")
    .WithRedisInsight(insightBuilder => insightBuilder.WithHostPort(insightPort))
    .WithDataVolume()
    .WithPersistence(interval: TimeSpan.FromMinutes(1));

var migrationsProject = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(connectionString)
    .WithReference(schedulerConnectionString)
    .WaitFor(connectionString)
    .WaitFor(schedulerConnectionString);

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(connectionString)
    .WithReference(schedulerConnectionString)
    .WaitFor(migrationsProject)
    .WithReference(cache)
    .WaitFor(cache);

var webPort = builder.Configuration.GetValue<int?>("Ports:Web");
builder.AddPnpmApp("web", "../Web")
    .WithHttpEndpoint(env: "PORT", port: webPort)
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
