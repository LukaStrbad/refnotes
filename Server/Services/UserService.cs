using Microsoft.EntityFrameworkCore;
using Server.Db;
using Server.Model;

namespace Server.Services;

public interface IUserService
{
    Task AddRefreshToken(UserRefreshToken userRefreshToken);
    Task DeleteRefreshToken(string username, string refreshToken);
    Task<UserRefreshToken?> GetRefreshToken(string username, string refreshToken);
}

public class UserService(RefNotesContext context) : IUserService
{
    public async Task AddRefreshToken(UserRefreshToken userRefreshToken)
    {
        await context.UserRefreshTokens.AddAsync(userRefreshToken);
        await context.SaveChangesAsync();
    }
    
    public async Task DeleteRefreshToken(string username, string refreshToken)
    {
        var item = await context.UserRefreshTokens.FirstOrDefaultAsync(x => x.Username == username && x.RefreshToken == refreshToken);
        if (item is null) return;

        context.UserRefreshTokens.Remove(item);
        await context.SaveChangesAsync();
    }
    
    public async Task<UserRefreshToken?> GetRefreshToken(string username, string refreshToken)
        => await context.UserRefreshTokens.FirstOrDefaultAsync(x => x.Username == username && x.RefreshToken == refreshToken);
}