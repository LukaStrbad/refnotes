using System.Diagnostics.CodeAnalysis;
using Api.Jobs;
using Api.Services;
using Api.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Api.Middlewares;
using Api.Services.Files;
using Api.Services.Redis;
using Api.Services.Schedulers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Primitives;
using ServiceDefaults;

namespace Api;

[ExcludeFromCodeCoverage]
public static class Configuration
{
    public static void RegisterServices(this IHostApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddOpenTelemetry();
        builder.RegisterScheduler();
        builder.Services.AddControllersWithViews();

        builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
        builder.Services.AddSingleton<IEncryptionKeyProvider, EncryptionKeyProvider>();
        builder.Services.AddSingleton(AppSettings.Initialize);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IDirectoryService, DirectoryService>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IFileServiceUtils, FileServiceUtils>();
        builder.Services.AddScoped<ISearchService, SearchService>();
        builder.Services.AddScoped<IUserGroupService, UserGroupService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IGroupPermissionService, GroupPermissionService>();
        builder.Services.AddScoped<IPublicFileService, PublicFileService>();
        builder.Services.AddScoped<IAppDomainService, AppDomainService>();
        builder.Services.AddScoped<IPublicFileImageService, PublicFileImageService>();
        builder.Services.AddScoped<IPublicFileScheduler, PublicFileScheduler>();
        builder.Services.AddScoped<IFavoriteService, FavoriteService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IEmailScheduler, EmailScheduler>();
        builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        builder.Services.AddScoped<IEmailConfirmService, EmailConfirmService>();
        builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
        builder.Services.AddScoped<IRedisLockProvider, RedisLockProvider>();
        builder.Services.AddScoped<IFileShareService, FileShareService>();

        builder.Services.AddTransient<IWebSocketMessageHandler, WebSocketMessageHandler>();
        builder.Services.AddTransient<IFileSyncService, FileSyncService>();
        builder.Services.AddTransient<IWebSocketFileSyncService, WebSocketFileSyncService>();
        builder.Services.AddTransient<ISmtpClient>(implementationFactory: _ => new SmtpClient());

        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<AppSettings>();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(appSettings.JwtPrivateKeyBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["accessToken"];
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("admin", p => p.RequireRole("administrator"));

        builder.Services.AddControllersWithViews();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenApi();
        }

        var maxFileSize = builder.Configuration.GetValue<long>("MaxFileSize");
        // Set 1 MB as the minimum max file size
        if (maxFileSize < 1024 * 1024)
            maxFileSize = 1024 * 1024;

        builder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxFileSize;
        });

        builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = maxFileSize; });
        builder.Services.AddMemoryCache();
        builder.AddRedisDistributedCache(connectionName: "cache");
    }

    private static void RegisterScheduler(this IHostApplicationBuilder builder)
    {
        builder.AddSchedulerServiceDefaults();
        builder.AddSchedulerHost();
        builder.Services.AddTransient<UpdatePublicFileImagesJob>();
    }

    public static void RegisterMiddlewares(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<AppSettings>();

        app.UseCors(builder => builder
            .AllowCredentials()
            .WithOrigins(appSettings.CorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
        );

        app.UseExceptionHandlerMiddleware();
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveTimeout = TimeSpan.FromSeconds(60)
        });
        app.MapControllers();
    }

    public static void RegisterAppServices(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        app.MapOpenApi();
        app.MapScalarApiReference(options => { options.AddDocument("v1", routePattern: "openapi/v1.json"); });
    }

    public static void RegisterAppSettingsReloadWatcher(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var appSettings = scope.ServiceProvider.GetRequiredService<AppSettings>();
        ChangeToken.OnChange(
            () => config.GetReloadToken(),
            () => appSettings.ReloadConfig()
        );
    }
}
