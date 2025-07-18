namespace Api.Services;

public interface IPasswordResetService
{
    Task<string> GenerateToken(int userId);
    Task<bool> ValidateToken(int userId, string token);
    Task DeleteTokensForUser(int userId);
}
