using Server;
using Server.Db;

var appConfig = Configuration.LoadAppConfig();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices(appConfig);

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.RegisterMiddlewares();

app.Run();
