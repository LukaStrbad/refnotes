using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Model;

namespace Server.Endpoints;

public class Admin : IEndpoint
{
    public static void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var admin = routes.MapGroup("/admin").RequireAuthorization("admin");

        admin.MapPost("/modifyRoles", async (User user, RefNotesContext db) =>
        {
            var existingUser = await db.Users.FindAsync(user.Id);
            if (existingUser is null)
            {
                return Results.NotFound();
            }

            existingUser.Roles = user.Roles;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        admin.MapGet("/listUsers", async (RefNotesContext db)
            => TypedResults.Ok(await db.Users.ToListAsync()));
    }
}