using System.Net;
using System.Security.Claims;
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
        group.MapPost("/adminToken", (HttpContext context, AuthService authService) =>
        {
            // Only allow localhost to get admin token
            var ip = context.Connection.RemoteIpAddress;
            if (ip is null || !IPAddress.IsLoopback(ip))
            {
                return Results.Unauthorized();
            }

            var user = new User(0, "admin", "admin", "admin@localhost", "admin123", ["administrator"]);
            return Results.Ok(authService.CreateToken(user));
        });

        group.MapPost("/login", async (UserCredentials credentials, RefNotesContext db, AuthService authService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == credentials.Username);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (user.Password != credentials.Password)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(authService.CreateToken(user));
        });

        group.MapPost("/register", async (User newUser, RefNotesContext db, AuthService authService) =>
        {
            // Clear roles to prevent user from setting roles
            newUser.Roles = [];

            // TODO: Hash password
            db.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Ok(authService.CreateToken(newUser));
        });
    }
}