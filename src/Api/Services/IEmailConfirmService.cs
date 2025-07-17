namespace Api.Services;

public interface IEmailConfirmService
{
    Task<string> GenerateToken(int userId);
    Task<bool> ConfirmEmail(string token, int userId);
}
