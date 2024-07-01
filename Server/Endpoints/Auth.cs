using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Model;
using Server.Services;

namespace Server.Endpoints;

public class Auth : IEndpoint
{
    public static void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth");
        group.MapPost("/adminToken",
            Results<Ok<string>, UnauthorizedHttpResult> (HttpContext context, AuthService authService) =>
            {
                // Only allow localhost to get admin token
                var ip = context.Connection.RemoteIpAddress;
                if (ip is null || !IPAddress.IsLoopback(ip))
                {
                    return TypedResults.Unauthorized();
                }

                var user = new User(0, "admin", "admin", "admin@localhost", "admin123", ["administrator"]);
                return TypedResults.Ok(authService.CreateToken(user));
            });

        group.MapPost("/login",
            async Task<Results<Ok<string>, NotFound, UnauthorizedHttpResult>>
                (UserCredentials credentials, RefNotesContext db, AuthService authService) =>
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

                return TypedResults.Ok(authService.CreateToken(user));
            });

        group.MapPost("/register", async (User newUser, RefNotesContext db, AuthService authService) =>
        {
            // Clear roles to prevent user from setting roles
            newUser.Roles = [];

            var passwordHasher = new PasswordHasher<UserCredentials>();
            newUser.Password = passwordHasher.HashPassword(new UserCredentials(newUser.Username, newUser.Password),
                newUser.Password);
            db.Add(newUser);
            await db.SaveChangesAsync();

            return TypedResults.Ok(authService.CreateToken(newUser));
        });
    }
}