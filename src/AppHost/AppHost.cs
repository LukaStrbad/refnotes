using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var postgresUsername = builder.AddParameter("postgres-username");
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", postgresUsername, postgresPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImageTag("18.0")
    .WithDataVolume(isReadOnly: false);

// Configure an optional exposed port
if (builder.Configuration.GetValue<int?>("Ports:Postgres") is { } port)
{
    postgres = postgres.WithHttpEndpoint(targetPort: 5432, name: "http", port: port, isProxied: false);
}

var mainDb = postgres.AddDatabase("main", "refnotes");
var schedulerDb = postgres.AddDatabase("scheduler", "scheduler");

var insightPort = builder.Configuration.GetValue<int?>("Ports:RedisInsight");
var cache = builder.AddRedis("cache")
    .WithRedisInsight(insightBuilder => insightBuilder.WithHostPort(insightPort))
    .WithDataVolume()
    .WithPersistence(interval: TimeSpan.FromMinutes(1));

var migrationsProject = builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(mainDb)
    .WithReference(schedulerDb)
    .WaitFor(mainDb)
    .WaitFor(schedulerDb);

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(mainDb)
    .WithReference(schedulerDb)
    .WaitFor(migrationsProject)
    .WithReference(cache)
    .WaitFor(cache);

var webPort = builder.Configuration.GetValue<int?>("Ports:Web");
builder.AddPnpmApp("web", "../Web", scriptName: "start:aspire")
    .WithHttpEndpoint(env: "PORT", port: webPort)
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
