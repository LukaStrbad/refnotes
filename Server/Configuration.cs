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
    private static string? _refnotesPath;
    private static AppConfiguration? _appConfig;
    private const string RootDir = "RefNotes";
    private const string ConfigFile = "config.json";
    private const string DefaultDataDir = "data";

    public static string RefnotesPath
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_refnotesPath);
            return _refnotesPath;
        }
        private set => _refnotesPath = value;
    }

    public static AppConfiguration AppConfig
    {
        get
        {
            if (_appConfig is not null) return _appConfig;
            throw new InvalidOperationException($"AppConfig not loaded. Call {nameof(LoadAppConfig)} method first.");
        }
        private set => _appConfig = value;
    }

    public static void RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();
        // TODO: Refactor controllers to save singletons to fields, and Scoped to methods
        builder.Services.AddDbContext<RefNotesContext>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<EncryptionService>();
        builder.Services.AddScoped<UserServiceRepository>();

        builder.Services
            .ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, ModelJsonSerializerContext.Default);
            });

        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
        {
            x.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(AppConfig.JwtPrivateKeyBytes),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("admin", p => p.RequireRole("administrator"));

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
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
        );
        
        app.UseExceptionHandlerMiddleware();
    }

    public static void RegisterEndpoints(this WebApplication app)
    {
        Admin.RegisterEndpoints(app);
        Auth.RegisterEndpoints(app);
        Browser.RegisterEndpoints(app);
    }

    public static void LoadAppConfig()
    {
        const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        RefnotesPath = Path.Join(path, RootDir);
        Directory.CreateDirectory(RefnotesPath);

        var configFile = Path.Join(RefnotesPath, ConfigFile);
        var config = new AppConfiguration();
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
        config.DataDir ??= Path.Join(RefnotesPath, DefaultDataDir);
        Directory.CreateDirectory(config.DataDir);

        AppConfig = config;
    }

    public class AppConfiguration
    {
        public string JwtPrivateKey { get; set; } = "";
        [JsonIgnore]
        public byte[] JwtPrivateKeyBytes => Encoding.UTF8.GetBytes(JwtPrivateKey);
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DataDir { get; set; }
    }
}