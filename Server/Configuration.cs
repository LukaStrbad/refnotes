using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Endpoints;
using Server.Middlewares;
using Server.Model;
using Server.Services;

namespace Server;

public static class Configuration
{
    private const string RootDir = "RefNotes";
    private const string ConfigFile = "config.json";
    private const string DefaultDataDir = "data";

    public static void RegisterServices(this WebApplicationBuilder builder, AppConfiguration appConfig)
    {
        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<RefNotesContext>();
        
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
        builder.Services.AddSingleton(appConfig);
        
        builder.Services.AddScoped<UserServiceRepository>();
        builder.Services.AddScoped<BrowserServiceRepository>();

        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(appConfig.JwtPrivateKeyBytes),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("admin", p => p.RequireRole("administrator"));

        builder.Services.AddControllersWithViews();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    public static void RegisterMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseCors(builder => builder
            .AllowCredentials()
            // TODO: Add a way to configure allowed origins
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
        );
        
        app.UseExceptionHandlerMiddleware();
        
        app.MapControllers();
    }

    public static void RegisterEndpoints(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        new Admin(services).RegisterEndpoints(app);
        new Auth(services).RegisterEndpoints(app);
    }

    public static AppConfiguration LoadAppConfig()
    {
        const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var baseDir = Path.Join(path, RootDir);
        Directory.CreateDirectory(baseDir);

        var configFile = Path.Join(baseDir, ConfigFile);
        var config = new AppConfiguration
        {
            BaseDir = baseDir
        };
        
        if (File.Exists(configFile))
        {
            var json = File.ReadAllText(configFile);
            config = JsonSerializer.Deserialize<AppConfiguration>(json);
            ArgumentNullException.ThrowIfNull(config);
        }
        else
        {
            Console.WriteLine("No configuration file found. Creating new one.");
            Console.WriteLine("You need to provide a JWT private key. The key must be at least 16 characters long.");
            Console.Write("Enter the JWT private key: ");
            var jwtPrivateKey = Console.ReadLine();
            ArgumentException.ThrowIfNullOrWhiteSpace(jwtPrivateKey);
            config.JwtPrivateKey = jwtPrivateKey;

            var json = JsonSerializer.Serialize(config);
            File.WriteAllText(configFile, json);
        }

        if (config.JwtPrivateKey is not { Length: >= 16 })
        {
            Console.WriteLine("Invalid JWT private key. Exiting.");
            Environment.Exit(1);
        }

        // Set default data directory and create it if it doesn't exist
        config.DataDir ??= Path.Join(baseDir, DefaultDataDir);
        Directory.CreateDirectory(config.DataDir);

        return config;
    }
}