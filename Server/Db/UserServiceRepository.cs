using Server.Model;

namespace Server.Db;

public class UserServiceRepository(RefNotesContext db)
{
    public UserRefreshToken AddUserRefreshToken(UserRefreshToken userRefreshToken)
    {
        db.UserRefreshTokens.Add(userRefreshToken);
        db.SaveChanges();
        return userRefreshToken;
    }

    public void DeleteUserRefreshToken(string username, string refreshToken)
    {
        var item = db.UserRefreshTokens.FirstOrDefault(x => x.Username == username && x.RefreshToken == refreshToken);
        if (item is null) return;

        db.UserRefreshTokens.Remove(item);
        db.SaveChanges();
    }

    public UserRefreshToken? GetSavedRefreshToken(string username, string refreshToken)
        => db.UserRefreshTokens.FirstOrDefault(x => x.Username == username && x.RefreshToken == refreshToken);
}