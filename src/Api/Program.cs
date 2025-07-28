using Api;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices();

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.RegisterMiddlewares();
app.RegisterAppServices();
app.RegisterAppSettingsReloadWatcher();

app.Run();
