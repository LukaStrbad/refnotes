using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Endpoints;
using Server.Middlewares;
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
        builder.AddDatabase(appConfig);
        builder.Services.AddSingleton(appConfig);

        builder.Services.AddScoped<IBrowserService, BrowserService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IEncryptionService, EncryptionService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<AuthService>();

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

    private static void AddDatabase(this WebApplicationBuilder builder, AppConfiguration appConfig)
    {
        var dbPath = Path.Join(appConfig.BaseDir, "refnotes.db");
        builder.Services.AddDbContext<RefNotesContext>(options =>
            options.UseSqlite($"Data Source={dbPath}")
        );
        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RefNotesContext>();
        db.Database.Migrate();
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
        new Auth(services.GetRequiredService<IUserService>(), services.GetRequiredService<AuthService>())
            .RegisterEndpoints(app);
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
        if (string.IsNullOrWhiteSpace(config.DataDir))
            config.DataDir = Path.Join(baseDir, DefaultDataDir);
        try
        {
            Directory.CreateDirectory(config.DataDir);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to create data directory: {e.Message}");
            Environment.Exit(1);
        }

        return config;
    }
}