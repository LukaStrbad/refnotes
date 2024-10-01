using Server;
using Server.Db;

var appConfig = Configuration.LoadAppConfig();

var builder = WebApplication.CreateBuilder(args);

builder.RegisterServices(appConfig);

var app = builder.Build();

app.RegisterMiddlewares();

app.Run();
