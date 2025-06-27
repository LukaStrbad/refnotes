using Api;
using ServiceDefaults;

var appConfig = Configuration.LoadAppConfig();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices(appConfig);
builder.AddSchedulerServiceDefaults();
builder.AddSchedulerHost();

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.RegisterMiddlewares();
app.RegisterAppServices();

app.Run();
