var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var mainDb = mysql.AddDatabase("refnotes");

builder.AddProject<Projects.Server>("apiService")
    .WithReference(mainDb)
    .WaitFor(mainDb);

builder.AddProject<Projects.MigrationService>("migrations")
    .WithReference(mainDb)
    .WaitFor(mainDb);

builder.Build().Run();