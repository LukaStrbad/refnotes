using Data.Model;

namespace Api.Services;

public interface IEmailConfirmService
{
    Task<string> GenerateToken(int userId);
    Task<(User? User, bool Success)> ConfirmEmail(string token);
}
