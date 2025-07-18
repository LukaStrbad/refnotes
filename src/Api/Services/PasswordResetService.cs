using Api.Exceptions;
using Data;
using Data.Model;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly RefNotesContext _context;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(RefNotesContext context, ILogger<PasswordResetService> logger)
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
            _logger.LogWarning("User with id {UserId} not found while generating password reset token", userId);
            throw new UserNotFoundException("User not found");
        }

        // Delete any existing tokens for the user
        await DeleteTokensForUser(userId);

        var passwordResetToken = new PasswordResetToken
        {
            Value = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        await _context.AddAsync(passwordResetToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated password reset token for user {UserId}", userId);

        return token;
    }

    public async Task<bool> ValidateToken(int userId, string token)
    {
        var passwordResetToken = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .FirstOrDefaultAsync(t => t.Value == token && t.ExpiresAt > DateTime.UtcNow);

        return passwordResetToken is not null;
    }

    public async Task DeleteTokensForUser(int userId)
    {
        await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
