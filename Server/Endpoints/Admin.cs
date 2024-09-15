using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Model;

namespace Server.Endpoints;

public class Admin : IEndpoint
{
    public static void RegisterEndpoints(WebApplication app)
    {
        var admin = app.MapGroup("/admin").RequireAuthorization("admin");

        admin.MapPost("/modifyRoles", async Task<Results<Ok<User>, NotFound>> (User user, RefNotesContext db) =>
        {
            var existingUser = await db.Users.FindAsync(user.Id);
            if (existingUser is null)
            {
                return TypedResults.NotFound();
            }

            existingUser.Roles = user.Roles;
            await db.SaveChangesAsync();

            return TypedResults.Ok(existingUser);
        });

        admin.MapGet("/listUsers", async (RefNotesContext db)
            => TypedResults.Ok(await db.Users.ToListAsync()));
    }
}