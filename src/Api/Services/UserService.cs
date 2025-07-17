using Api.Exceptions;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class UserService : IUserService
{
    private readonly RefNotesContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // UserService should be a scoped dependency, so _user will be different for every request
    private User? _user;

    public UserService(RefNotesContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<User> GetCurrentUser()
    {
        var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
        if (claimsPrincipal?.Identity?.Name is not { } name || name == "")
        {
            throw new NoNameException();
        }

        if (_user is not null)
        {
            if (_context.Entry(_user).State == EntityState.Detached)
                _context.Users.Attach(_user);

            return _user;
        }

        if (!claimsPrincipal.Identity.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        _user = await _context.Users.FirstOrDefaultAsync(u => u.Username == name);

        if (_user is null)
        {
            throw new UserNotFoundException($"User ${name} not found.");
        }

        return _user;
    }

    public async Task<User> GetByUsername(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            throw new UserNotFoundException($"User {username} not found.");
        }
        
        return user;
    }
}
