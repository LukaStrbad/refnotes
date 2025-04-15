using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Middlewares;
using Server.Services;
using Server.Utils;

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
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IBrowserService, BrowserService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IEncryptionService, EncryptionService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ServiceUtils>();

        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(appConfig.JwtPrivateKeyBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("admin", p => p.RequireRole("administrator"));

        builder.Services.AddControllersWithViews();

        builder.Services.AddOpenApi();

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 1024 * 1024 * 1024; // 1 GB
        });

        builder.Services.AddMemoryCache();
    }

    private static void AddDatabase(this WebApplicationBuilder builder, AppConfiguration appConfig)
    {
        var connectionString = builder.Configuration["Db:ConnectionString"];
        ArgumentNullException.ThrowIfNull(connectionString);
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        builder.Services.AddDbContext<RefNotesContext>(options =>
            options.UseMySql(connectionString, serverVersion)
        );

        using var scope = builder.Services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RefNotesContext>();
        db.Database.Migrate();
    }

    public static void RegisterMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
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

    public static AppConfiguration LoadAppConfig()
    {
        // Get user's home directory
        const Environment.SpecialFolder folder = Environment.SpecialFolder.UserProfile;
        var userFolder = Environment.GetFolderPath(folder);
        var userConfigDir = Path.Join(userFolder, ".config", "refnotes");

        // Check if CONFIG_PATH environment variable is set
        var configPath = Environment.GetEnvironmentVariable("REFNOTES_CONFIG_PATH");

        // Use CONFIG_PATH if set, otherwise use user's home directory
        var baseDir = string.IsNullOrWhiteSpace(configPath)
            ? userConfigDir
            : configPath;

        Console.WriteLine($"Using configuration path: {baseDir}");

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
            config.BaseDir = baseDir;
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