using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Server.Db;
using Server.Model;
using Server.Services;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Server.Endpoints;

public class Auth : IEndpoint
{
    private readonly RefNotesContext _db;
    private readonly AuthService _authService;

    /// <summary>
    /// Gets the cookie options for the refresh token.
    /// </summary>
    private static readonly CookieOptions HttpOnlyCookieOptions = new()
    {
        Domain = "localhost",
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        HttpOnly = true,
        Secure = false
    };

    public Auth(IServiceProvider services)
    {
        _db = services.GetRequiredService<RefNotesContext>();
        _authService = services.GetRequiredService<AuthService>();
    }

    public void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login",
            async Task<Results<Ok<string>, NotFound, UnauthorizedHttpResult>>
                (UserCredentials credentials, UserServiceRepository userServiceRepository, HttpContext httpContext) =>
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == credentials.Username);
                if (user is null)
                {
                    return TypedResults.NotFound();
                }

                var passwordHasher = new PasswordHasher<UserCredentials>();
                var result = passwordHasher.VerifyHashedPassword(credentials, user.Password, credentials.Password);

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (result)
                {
                    case PasswordVerificationResult.Failed:
                        return TypedResults.Unauthorized();
                    case PasswordVerificationResult.SuccessRehashNeeded:
                        user.Password = passwordHasher.HashPassword(credentials, credentials.Password);
                        await _db.SaveChangesAsync();
                        break;
                }

                var accessToken = CreateAccessToken(_authService, user, userServiceRepository, httpContext);

                return TypedResults.Ok(accessToken);
            });

        group.MapPost("/refresh",
            async Task<Results<Ok<string>, BadRequest<string>, ProblemHttpResult>>
                ([FromBody] string accessToken, UserServiceRepository userServiceRepository, HttpContext httpContext) =>
            {
                var principal = _authService.GetPrincipalFromExpiredToken(accessToken);
                var name = principal.Identity?.Name;
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == name);

                var refreshToken = httpContext.Request.Cookies["refreshToken"];

                if (user is null || refreshToken is null)
                {
                    return TypedResults.BadRequest("No user or refresh token found");
                }

                var savedRefreshToken = userServiceRepository.GetSavedRefreshToken(user.Username, refreshToken);
                if (savedRefreshToken is null || savedRefreshToken.IsExpired)
                {
                    // Unauthorized
                    return TypedResults.Problem("Refresh token is invalid or expired", statusCode: 401);
                }

                userServiceRepository.DeleteUserRefreshToken(user.Username, refreshToken);
                var newAccessToken = CreateAccessToken(_authService, user, userServiceRepository, httpContext);

                return TypedResults.Ok(newAccessToken);
            });

        group.MapPost("/register",
            async (User newUser, UserServiceRepository userServiceRepository, HttpContext httpContext) =>
            {
                // Clear roles to prevent user from setting roles
                newUser.Roles = [];

                var passwordHasher = new PasswordHasher<UserCredentials>();
                newUser.Password = passwordHasher.HashPassword(new UserCredentials(newUser.Username, newUser.Password),
                    newUser.Password);
                _db.Add(newUser);
                await _db.SaveChangesAsync();

                var accessToken = CreateAccessToken(_authService, newUser, userServiceRepository, httpContext);

                return TypedResults.Ok(accessToken);
            });
    }

    private static string CreateAccessToken(AuthService authService, User user,
        UserServiceRepository userServiceRepository, HttpContext httpContext)
    {
        var tokens = authService.CreateTokens(user);
        userServiceRepository.AddUserRefreshToken(new UserRefreshToken
        {
            Username = user.Username,
            RefreshToken = tokens.RefreshToken.Token,
            ExpiryTime = tokens.RefreshToken.ExpiryTime
        });

        var options = HttpOnlyCookieOptions;
        options.Expires = tokens.RefreshToken.ExpiryTime;
        httpContext.Response.Cookies.Append("refreshToken", tokens.RefreshToken.Token, HttpOnlyCookieOptions);
        return tokens.AccessToken;
    }
}