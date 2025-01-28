using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Db.Model;
using Server.Exceptions;

namespace Server.Services;

public abstract class BaseService(RefNotesContext context)
{
    protected readonly RefNotesContext Context = context;

    protected async Task<User> GetUser(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity?.Name is not { } name || name == "")
        {
            throw new NoNameException();
        }

        var user = await Context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return user;
    }
}