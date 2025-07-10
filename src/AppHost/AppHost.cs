var builder = DistributedApplication.CreateBuilder(args);

var connectionString = builder.AddConnectionString("main");
var schedulerConnectionString = builder.AddConnectionString("scheduler");

var cache = builder.AddRedis("cache")
    .WithRedisInsight()
    .WithDataVolume()
    .WithPersistence(interval: TimeSpan.FromMinutes(5));

var migrationsProject = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(connectionString)
    .WithReference(schedulerConnectionString)
    .WaitFor(connectionString)
    .WaitFor(schedulerConnectionString);

builder.AddProject<Projects.Api>("apiService")
    .WithReference(connectionString)
    .WithReference(schedulerConnectionString)
    .WaitFor(migrationsProject)
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
