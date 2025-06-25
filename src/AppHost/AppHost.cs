var builder = DistributedApplication.CreateBuilder(args);

var connectionString = builder.AddConnectionString("main");

builder.AddProject<Projects.Api>("apiService")
    .WithReference(connectionString)
    .WaitFor(connectionString);

builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(connectionString)
    .WaitFor(connectionString);

builder.Build().Run();