using Api.Exceptions;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface IUserService
{
    /// <summary>
    /// Gets currently logged-in user
    /// </summary>
    Task<User> GetUser();
}

public class UserService(
    RefNotesContext context,
    IHttpContextAccessor httpContextAccessor) : IUserService
{
    // UserService should be a scoped dependency, so _user will be different for every request
    private User? _user;

    public async Task<User> GetUser()
    {
        var claimsPrincipal = httpContextAccessor.HttpContext?.User;
        if (claimsPrincipal?.Identity?.Name is not { } name || name == "")
        {
            throw new NoNameException();
        }

        if (_user is not null)
        {
            if (context.Entry(_user).State == EntityState.Detached)
                context.Users.Attach(_user);

            return _user;
        }

        if (!claimsPrincipal.Identity.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        _user = await context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (_user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return _user;
    }
}