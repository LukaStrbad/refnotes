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
    private readonly IUserService _userService;
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

    public Auth(IUserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    public void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/login",
            async Task<Results<Ok<string>, NotFound, UnauthorizedHttpResult>>
                (UserCredentials credentials, RefNotesContext db, HttpContext httpContext) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == credentials.Username);
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
                        await db.SaveChangesAsync();
                        break;
                }

                var accessToken = await CreateAccessToken(_authService, user, _userService, httpContext);

                return TypedResults.Ok(accessToken);
            });

        group.MapPost("/refresh",
            async Task<Results<Ok<string>, BadRequest<string>, ProblemHttpResult>>
                ([FromBody] string accessToken, RefNotesContext db, HttpContext httpContext) =>
            {
                var principal = _authService.GetPrincipalFromExpiredToken(accessToken);
                var name = principal.Identity?.Name;
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == name);

                var refreshToken = httpContext.Request.Cookies["refreshToken"];

                if (user is null || refreshToken is null)
                {
                    return TypedResults.BadRequest("No user or refresh token found");
                }

                var savedRefreshToken = await _userService.GetRefreshToken(user.Username, refreshToken);
                if (savedRefreshToken is null || savedRefreshToken.IsExpired)
                {
                    // Unauthorized
                    return TypedResults.Problem("Refresh token is invalid or expired", statusCode: 401);
                }

                await _userService.DeleteRefreshToken(user.Username, refreshToken);
                var newAccessToken = await CreateAccessToken(_authService, user, _userService, httpContext);

                return TypedResults.Ok(newAccessToken);
            });

        group.MapPost("/register",
            async (User newUser, RefNotesContext db, HttpContext httpContext) =>
            {
                // Clear roles to prevent user from setting roles
                newUser.Roles = [];

                var passwordHasher = new PasswordHasher<UserCredentials>();
                newUser.Password = passwordHasher.HashPassword(new UserCredentials(newUser.Username, newUser.Password),
                    newUser.Password);
                db.Add(newUser);
                await db.SaveChangesAsync();

                var accessToken = await CreateAccessToken(_authService, newUser, _userService, httpContext);

                return TypedResults.Ok(accessToken);
            });
    }

    private static async Task<string> CreateAccessToken(AuthService authService, User user, IUserService userService,
        HttpContext httpContext)
    {
        var tokens = authService.CreateTokens(user);
        await userService.AddRefreshToken(new UserRefreshToken
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