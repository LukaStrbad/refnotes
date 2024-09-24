using Server;
using Server.Db;

var appConfig = Configuration.LoadAppConfig();

var builder = WebApplication.CreateBuilder(args);
using var db = new RefNotesContext(appConfig);
db.Database.EnsureCreated();

builder.RegisterServices(appConfig);

var app = builder.Build();

app.RegisterMiddlewares();
app.RegisterEndpoints();

app.Run();
