using Api.Exceptions;
using Api.Model;
using Data;
using Data.Model;
using Microsoft.AspNetCore.Identity;
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

    public async Task<User> EditUser(int userId, EditUserRequest details)
    {
        var existingUser = await _context.Users
            .Where(u => u.Id != userId) // Ensure we don't check the user being edited
            .FirstOrDefaultAsync(u => u.Username == details.NewUsername);
        if (existingUser is not null)
        {
            throw new UserExistsException("User already exists with this username.");
        }

        var user = await _context.Users.FindAsync(userId) ??
                   throw new UserNotFoundException($"User with ID {userId} not found.");

        user.Name = details.NewName;
        user.Username = details.NewUsername;
        user.Email = details.NewEmail;
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UnconfirmEmail(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            throw new UserNotFoundException($"User with ID {userId} not found.");
        }

        user.EmailConfirmed = false;
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePassword(UserCredentials newCredentials)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == newCredentials.Username);
        if (user is null)
        {
            throw new UserNotFoundException($"User {newCredentials.Username} not found.");
        }

        var passwordHasher = new PasswordHasher<UserCredentials>();
        user.Password = passwordHasher.HashPassword(newCredentials, newCredentials.Password);
        await _context.SaveChangesAsync();
    }
}
