using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Server.Db;
using Server.Endpoints;
using Server.Model;
using Server.Services;

namespace Server;

public static class Configuration
{
    public static void RegisterServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<RefNotesContext>();
        builder.Services.AddSingleton<AuthService>();
        
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
                IssuerSigningKey = new SymmetricSecurityKey("bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@"u8.ToArray()),
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
    }

    public static void RegisterEndpoints(this WebApplication app)
    {
        Admin.RegisterEndpoints(app);
        Auth.RegisterEndpoints(app);
        Browser.RegisterEndpoints(app);
    }
}