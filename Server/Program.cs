using Server;
using Server.Db;

Configuration.LoadAppConfig();

var builder = WebApplication.CreateBuilder(args);
using var db = new RefNotesContext();
db.Database.EnsureCreated();

builder.RegisterServices();

var app = builder.Build();

app.RegisterMiddlewares();
app.RegisterEndpoints();

app.Run();
