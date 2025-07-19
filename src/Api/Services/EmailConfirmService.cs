using Api.Exceptions;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class EmailConfirmService : IEmailConfirmService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<EmailConfirmService> _logger;

    public EmailConfirmService(RefNotesContext context, ILogger<EmailConfirmService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateToken(int userId)
    {
        var token = Guid.NewGuid().ToString();
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("User with ID {UserId} not found while generating email confirmation token", userId);
            throw new UserNotFoundException("User not found");
        }

        // Delete any existing tokens for the user
        await DeleteTokensForUser(userId);

        var emailConfirmToken = new EmailConfirmToken
        {
            Value = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        await _context.AddAsync(emailConfirmToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated email confirmation token for user {UserId}", userId);

        return token;
    }

    public async Task<(User? User, bool Success)> ConfirmEmail(string token)
    {
        var emailConfirmToken = await _context.EmailConfirmTokens
            .Include(emailConfirmToken => emailConfirmToken.User)
            .FirstOrDefaultAsync(t => t.Value == token && t.ExpiresAt > DateTime.UtcNow);

        if (emailConfirmToken is null)
            return (null, false);

        var user = emailConfirmToken.User;
        if (user is null)
        {
            _logger.LogCritical("User not found for email confirmation token model with ID {EmailConfirmTokenId}",
                emailConfirmToken.Id);
            throw new UserNotFoundException("User not found");
        }

        user.EmailConfirmed = true;
        _context.EmailConfirmTokens.Remove(emailConfirmToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email confirmed for user {UserId}", user.Id);

        return (user, true);
    }

    public async Task DeleteTokensForUser(int userId)
    {
        await _context.EmailConfirmTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
